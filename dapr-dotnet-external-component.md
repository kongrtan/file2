# Dapr External PubSub Component (.NET 7 Sample)

이 문서는 Dapr에서 사용할 수 있는 **외부 프로세스 PubSub 컴포넌트**를 .NET 7으로 구현한 예제입니다.  
Go에 종속되지 않고 독립 실행형 gRPC 서버로 동작하며, Dapr은 이 컴포넌트와 gRPC로 통신합니다.

---

## 🗂 프로젝트 구조

```
MyBrokerComponent/
 ├── MyBrokerComponent.sln
 └── MyBrokerComponent/
     ├── MyBrokerComponent.csproj
     ├── Program.cs
     ├── MyBrokerPubSub.cs
     └── Protos/
         └── dapr.proto
components/
 └── mybroker-pubsub.yaml
```

---

## 📦 NuGet 패키지

```bash
dotnet add package Grpc --version 2.46.6
dotnet add package Grpc.Tools --version 2.46.6
dotnet add package Google.Protobuf --version 3.21.12
```

---

## 📜 MyBrokerComponent.sln

```txt
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.0.31903.59
MinimumVisualStudioVersion = 10.0.40219.1
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "MyBrokerComponent", "MyBrokerComponent/MyBrokerComponent.csproj", "{11111111-2222-3333-4444-555555555555}"
EndProject
Global
    GlobalSection(SolutionConfigurationPlatforms) = preSolution
        Debug|Any CPU = Debug|Any CPU
        Release|Any CPU = Release|Any CPU
    EndGlobalSection
    GlobalSection(ProjectConfigurationPlatforms) = postSolution
        {11111111-2222-3333-4444-555555555555}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
        {11111111-2222-3333-4444-555555555555}.Debug|Any CPU.Build.0 = Debug|Any CPU
        {11111111-2222-3333-4444-555555555555}.Release|Any CPU.ActiveCfg = Release|Any CPU
        {11111111-2222-3333-4444-555555555555}.Release|Any CPU.Build.0 = Release|Any CPU
    EndGlobalSection
EndGlobal
```

---

## 📜 MyBrokerComponent/MyBrokerComponent.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net7.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Grpc" Version="2.46.6" />
    <PackageReference Include="Grpc.Tools" Version="2.46.6">
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Google.Protobuf" Version="3.21.12" />
  </ItemGroup>

  <ItemGroup>
    <Protobuf Include="Protos\dapr.proto" GrpcServices="Server" ProtoRoot="Protos" />
  </ItemGroup>

</Project>
```

---

## 📜 MyBrokerComponent/Protos/dapr.proto

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

## 📜 MyBrokerComponent/MyBrokerPubSub.cs

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

## 📜 MyBrokerComponent/Program.cs

```csharp
using System;
using Grpc.Core;

namespace MyBrokerComponent
{
    class Program
    {
        const int Port = 50001;

        static void Main(string[] args)
        {
            var server = new Server
            {
                Services = { Dapr.Proto.Components.V1.PubSub.BindService(new MyBrokerPubSub()) },
                Ports = { new ServerPort("0.0.0.0", Port, ServerCredentials.Insecure) }
            };

            server.Start();
            Console.WriteLine($"[MyBrokerComponent] PubSub external component listening on {Port}");
            Console.WriteLine("Press Ctrl+C to exit...");

            AppDomain.CurrentDomain.ProcessExit += async (s, e) => await server.ShutdownAsync();

            System.Threading.Thread.Sleep(-1);
        }
    }
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
    value: "127.0.0.1:50001"
```

---

## 🚀 실행 방법

1. 위 구조대로 파일 생성
2. `dotnet build`
3. `dotnet run` (외부 컴포넌트 실행)
4. Dapr 앱 실행 시:
   ```bash
   dapr run --app-id myapp --components-path ./components -- dotnet run
   ```
5. 메시지 발행 테스트:
   ```bash
   dapr publish --pubsub mybroker-pubsub --topic demo --data '{"hello":"world"}'
   ```
6. 외부 컴포넌트 콘솔 로그 확인

---

✅ 이 샘플은 최소 동작 가능한 .NET 기반 외부 PubSub 컴포넌트 예제입니다.
