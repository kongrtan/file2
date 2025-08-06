
# aspir8 도입 및 사용 예제

## 📚 목차

1. [aspir8 설치 방법](#-aspir8-설치-방법)
2. [예제 시나리오](#-예제-시나리오)
3. [aspir8 실행](#-aspir8-실행)
4. [생성된 YAML 예시](#-생성된-yaml-예시)
5. [배포 방법](#-배포-방법)
6. [참고 팁](#-참고-팁)

# aspir8 도입 및 사용 예제

## ✅ aspir8 설치 방법

```bash
dotnet tool install -g aspir8
```

> 업데이트:
```bash
dotnet tool update -g aspir8
```

---

## 🧪 예제 시나리오

Aspire 프로젝트 구성:

- AppHost (`AppHost`)
- Web API 서비스 (`MyApi`)
- Redis 캐시 (`redis`)

### AppHost의 `Program.cs`

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis");

builder.AddProject<Projects.MyApi>("myapi")
       .WithReference(redis);

builder.Build().Run();
```

---

## 🚀 aspir8 실행

```bash
aspir8 --output-format kubernetes-yaml --output-dir ./k8s
```

> `./k8s` 디렉터리에 Kubernetes YAML 파일들이 생성됩니다.

---

## 📄 생성된 YAML 예시

### 🔹 `myapi-deployment.yaml`

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: myapi
spec:
  replicas: 1
  selector:
    matchLabels:
      app: myapi
  template:
    metadata:
      labels:
        app: myapi
    spec:
      containers:
        - name: myapi
          image: myapi:dev
          ports:
            - containerPort: 8080
          env:
            - name: Redis__ConnectionString
              value: "redis:6379"
```

### 🔹 `myapi-service.yaml`

```yaml
apiVersion: v1
kind: Service
metadata:
  name: myapi
spec:
  selector:
    app: myapi
  ports:
    - port: 80
      targetPort: 8080
```

### 🔹 `redis-deployment.yaml`

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: redis
spec:
  replicas: 1
  selector:
    matchLabels:
      app: redis
  template:
    metadata:
      labels:
        app: redis
    spec:
      containers:
        - name: redis
          image: redis:7
          ports:
            - containerPort: 6379
```

### 🔹 `redis-service.yaml`

```yaml
apiVersion: v1
kind: Service
metadata:
  name: redis
spec:
  selector:
    app: redis
  ports:
    - port: 6379
```

---

## 📦 배포 방법

```bash
kubectl apply -f ./k8s
```

---

## 💡 참고 팁

- aspir8은 AppHost의 설정을 기반으로 Kubernetes YAML을 자동 생성합니다.
- 생성된 이미지 (`:dev`)는 로컬 빌드 기준이므로 실제 배포 시 레지스트리 푸시 필요
- Ingress, PVC, Secrets 등은 환경에 따라 수동 설정해야 합니다.
