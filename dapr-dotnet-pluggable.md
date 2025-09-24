# Dapr .NET Pluggable Components - 개발 & Kubernetes 배포 가이드

## 1. 개발 환경 (로컬)

로컬에서는 `dapr run` 명령어를 통해 sidecar + 애플리케이션 + 컴포넌트를
동시에 실행한다.

``` bash
dapr run --app-id myapp --resources-path ./components dotnet run --project MyApp
```

### 예시: State Store 구현

``` csharp
using Dapr.PluggableComponents.Components;
using Dapr.PluggableComponents.Components.StateStore;
using Dapr.PluggableComponents;

public class MyStateStore : StateStoreBase
{
    private readonly Dictionary<string, byte[]> _store = new();

    public override ValueTask InitAsync(MetadataRequest request, CancellationToken ct = default)
    {
        Console.WriteLine("MyStateStore initialized");
        return ValueTask.CompletedTask;
    }

    public override ValueTask<StateStoreGetResponse> GetAsync(StateStoreGetRequest request, CancellationToken ct = default)
    {
        _store.TryGetValue(request.Key, out var value);
        return ValueTask.FromResult(new StateStoreGetResponse { Data = value });
    }

    public override ValueTask SetAsync(StateStoreSetRequest request, CancellationToken ct = default)
    {
        _store[request.Key] = request.Value;
        return ValueTask.CompletedTask;
    }

    public override ValueTask DeleteAsync(StateStoreDeleteRequest request, CancellationToken ct = default)
    {
        _store.Remove(request.Key);
        return ValueTask.CompletedTask;
    }
}

class Program
{
    static Task Main(string[] args)
    {
        return DaprPluggableComponentsRuntime.RunAsync(components =>
        {
            components.RegisterStateStore<MyStateStore>("mymemory");
        });
    }
}
```

### 컴포넌트 YAML

``` yaml
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: mystate
spec:
  type: state.mymemory
  version: v1
  metadata: []
```

------------------------------------------------------------------------

## 2. PubSub 컴포넌트 예시

``` csharp
using Dapr.PluggableComponents;
using Dapr.PluggableComponents.Components.PubSub;
using System.Threading.Channels;

public class MyPubSub : PubSubBase
{
    private readonly Channel<PubSubPublishRequest> _channel = Channel.CreateUnbounded<PubSubPublishRequest>();

    public override ValueTask InitAsync(MetadataRequest request, CancellationToken ct = default)
    {
        Console.WriteLine("MyPubSub initialized");
        return ValueTask.CompletedTask;
    }

    public override ValueTask<PubSubPublishResponse> PublishAsync(PubSubPublishRequest request, CancellationToken ct = default)
    {
        Console.WriteLine($"Published topic={request.Topic} data={System.Text.Encoding.UTF8.GetString(request.Data)}");
        _channel.Writer.TryWrite(request);
        return ValueTask.FromResult(new PubSubPublishResponse());
    }

    public override async IAsyncEnumerable<PubSubSubscribedMessage> SubscribeAsync(PubSubSubscriptionRequest request, [EnumeratorCancellation] CancellationToken ct = default)
    {
        Console.WriteLine($"Subscribed to topic={request.Topic}");

        await foreach (var message in _channel.Reader.ReadAllAsync(ct))
        {
            if (message.Topic == request.Topic)
            {
                yield return new PubSubSubscribedMessage
                {
                    Data = message.Data,
                    ContentType = "application/json",
                    Topic = message.Topic
                };
            }
        }
    }
}

class Program
{
    static Task Main(string[] args)
    {
        return DaprPluggableComponentsRuntime.RunAsync(components =>
        {
            components.RegisterPubSub<MyPubSub>("mypubsub");
        });
    }
}
```

### PubSub 컴포넌트 YAML

``` yaml
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: mypubsub
spec:
  type: pubsub.mypubsub
  version: v1
  metadata: []
```

------------------------------------------------------------------------

## 3. Kubernetes 배포

### 핵심 개념

-   **Dapr sidecar**: 자동 주입 (injector)
-   **플러그블 컴포넌트**: 같은 Pod 안에 컨테이너로 포함
-   **Unix Domain Socket(UDS)**: `emptyDir` 볼륨을 통해 공유

### Deployment 예시

``` yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: myapp
spec:
  replicas: 1
  selector:
    matchLabels:
      app: myapp
  template:
    metadata:
      labels:
        app: myapp
      annotations:
        dapr.io/enabled: "true"
        dapr.io/app-id: "myapp"
        dapr.io/app-port: "5000"
        dapr.io/inject-pluggable-components: "true"
    spec:
      volumes:
      - name: dapr-sockets
        emptyDir: {}
      containers:
      - name: myapp
        image: myregistry/myapp:latest
        ports:
        - containerPort: 5000
        volumeMounts:
        - name: dapr-sockets
          mountPath: /tmp/dapr-components-sockets
      - name: mystate
        image: myregistry/mystate:latest
        volumeMounts:
        - name: dapr-sockets
          mountPath: /tmp/dapr-components-sockets
```

### 배포 순서

1.  Docker 이미지 빌드 & 푸시

    ``` bash
    docker build -t myregistry/mystate:latest .
    docker push myregistry/mystate:latest
    ```

2.  컴포넌트 리소스 등록

    ``` bash
    kubectl apply -f mypubsub.yaml
    ```

3.  애플리케이션 + 컴포넌트 Pod 배포

    ``` bash
    kubectl apply -f myapp-deployment.yaml
    ```

### 테스트

``` bash
kubectl exec -it deploy/myapp -c myapp --   curl http://localhost:3500/v1.0/publish/mypubsub/mytopic -d '{"hello":"k8s"}'
```

------------------------------------------------------------------------

## 4. 요약

-   개발 환경: `dapr run --resources-path ./components`
-   Kubernetes:
    -   Component YAML 등록 (`kubectl apply`)
    -   Deployment에 컴포넌트 컨테이너 포함
    -   `dapr.io/inject-pluggable-components: "true"` 설정
    -   `emptyDir` 볼륨으로 UDS 공유
