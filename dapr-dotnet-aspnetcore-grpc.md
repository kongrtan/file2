# Dapr External PubSub Component (.NET 7, ASP.NET Core gRPC + Reflection)

이 문서는 Dapr에서 사용할 수 있는 **외부 프로세스 PubSub 컴포넌트**를 .NET 7 ASP.NET Core gRPC로 구현한 예제입니다.  
gRPC Reflection을 활성화하여 `grpcurl list` / `describe`가 동작하도록 구성되어 있습니다.

---

## 🗂 프로젝트 구조

```
MyBrokerComponent/
 ├── MyBrokerComponent.csproj
 ├── Program.cs
 ├── MyBrokerPubSub.cs
 └── Protos/
     └── dapr.proto
components/
 └── mybroker-pubsub.yaml
```

---

## 📦 MyBrokerComponent.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net7.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Grpc.AspNetCore" Version="2.52.0" />
    <PackageReference Include="Grpc.Tools" Version="2.52.0">
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Google.Protobuf" Version="3.21.12" />
  </ItemGroup>

  <ItemGroup>
    <Protobuf Include="Protos\dapr.proto" GrpcServices="Server" />
  </ItemGroup>

</Project>
```

---

## 📜 Program.cs

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// gRPC 서비스 등록
builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();

var app = builder.Build();

// gRPC 서비스 맵핑
app.MapGrpcService<MyBrokerPubSub>();

// Reflection 서비스 활성화 (개발용)
#if DEBUG
app.MapGrpcReflectionService();
#endif

app.Run();
```

---

## 📜 MyBrokerPubSub.cs

```csharp
using System;
using System.Threading.Tasks;
using Grpc.Core;
using Dapr.Proto.Components.V1;

namespace MyBrokerComponent
{
    public class MyBrokerPubSub : PubSub.PubSubBase
    {
        public override Task<InitResponse> Init(InitRequest request, ServerCallContext context)
        {
            Console.WriteLine("Init called with metadata:");
            foreach (var kv in request.Metadata)
            {
                Console.WriteLine($"{kv.Key}={kv.Value}");
            }
            return Task.FromResult(new InitResponse());
        }

        public override Task<PublishResponse> Publish(PublishRequest request, ServerCallContext context)
        {
            Console.WriteLine($"Publish called. topic={request.Topic}, data={System.Text.Encoding.UTF8.GetString(request.Data.ToByteArray())}");
            return Task.FromResult(new PublishResponse());
        }

        public override async Task Subscribe(SubscribeRequest request, IServerStreamWriter<NewMessage> responseStream, ServerCallContext context)
        {
            Console.WriteLine($"Subscribe called. topic={request.Topic}");

            while (!context.CancellationToken.IsCancellationRequested)
            {
                var msg = new NewMessage
                {
                    Topic = request.Topic,
                    Data = Google.Protobuf.ByteString.CopyFromUtf8("Hello from MyBrokerPubSub")
                };
                await responseStream.WriteAsync(msg);
                await Task.Delay(3000, context.CancellationToken);
            }
        }
    }
}
```

---

## 📜 Protos/dapr.proto

```proto
syntax = "proto3";

package dapr.proto.components.v1;

service PubSub {
  rpc Init (InitRequest) returns (InitResponse);
  rpc Publish (PublishRequest) returns (PublishResponse);
  rpc Subscribe (SubscribeRequest) returns (stream NewMessage);
}

message InitRequest {
  map<string, string> metadata = 1;
}
message InitResponse {}

message PublishRequest {
  string topic = 1;
  bytes data = 2;
}
message PublishResponse {}

message SubscribeRequest {
  string topic = 1;
}
message NewMessage {
  string topic = 1;
  bytes data = 2;
}
```

---

## 📜 components/mybroker-pubsub.yaml

```yaml
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: mybroker-pubsub
spec:
  type: pubsub.mybroker
  version: v1
  metadata:
  - name: endpoint
    value: "localhost:5000"
```

> ASP.NET Core gRPC 서버 기본 포트: 5000

---

## 🚀 실행 및 테스트

1. MyBrokerComponent 빌드/실행

```bash
dotnet run
```

2. Reflection 활성화 확인

```bash
grpcurl -plaintext localhost:5000 list
grpcurl -plaintext localhost:5000 describe dapr.proto.components.v1.PubSub
```

- 서비스와 메서드 목록이 정상 출력됩니다.

3. Dapr 앱에서 publish/subscribe 테스트

```bash
dapr publish --pubsub mybroker-pubsub --topic demo --data '{"hello":"world"}'
```

---

✅ 이 샘플은 **ASP.NET Core gRPC + Reflection**을 지원하여 grpcurl 디버깅 가능하며, 외부 Dapr PubSub 컴포넌트로 바로 사용 가능합니다.
