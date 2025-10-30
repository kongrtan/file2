# Dapr + TIBCO RV Gateway Chat ����

---

## 1. ��Ű��ó ��õ
- Dapr Service Invocation �� TIBCO RV ������ ����
- Gateway Adapter ����
- ConfigMap/Secret ���� ���񽺺� rvd ����
- Kubernetes ���� �� ���񽺺� Deployment/Service ���� ����

---

## 2. Kubernetes Manifests
### Namespace
```yaml
apiVersion: v1
kind: Namespace
metadata:
  name: rv-integration
```

### ConfigMap (RV ��������)
```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: rv-connections
  namespace: rv-integration
data:
  rv-connections.yaml: |
    inventory:
      daemon: "tcp:7600"
      network: "10.0.0.10"
      service: "7500"
      subject: "inventory.request"
      replySubjectPrefix: "inventory.reply"
```

### Secret (���񽺺� credentials)
```yaml
apiVersion: v1
kind: Secret
metadata:
  name: rv-credentials
  namespace: rv-integration
type: Opaque
data:
  inventory_username: aW52ZW50X3VzZXI=
  inventory_password: c2VjcmV0X2luYw==
```

### Deployment & Service (rv-gateway)
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: rv-gateway
  namespace: rv-integration
  labels: { app: rv-gateway }
spec:
  replicas: 2
  selector: { matchLabels: { app: rv-gateway } }
  template:
    metadata:
      labels: { app: rv-gateway }
      annotations:
        dapr.io/enabled: "true"
        dapr.io/app-id: "rv-gateway"
        dapr.io/app-port: "8080"
    spec:
      containers:
      - name: rv-gateway
        image: myregistry/rv-gateway:latest
        ports:
        - containerPort: 8080
        volumeMounts:
        - name: rv-config
          mountPath: /etc/rv
          readOnly: true
      volumes:
      - name: rv-config
        configMap:
          name: rv-connections
```

### Service
```yaml
apiVersion: v1
kind: Service
metadata:
  name: rv-gateway
  namespace: rv-integration
spec:
  selector: { app: rv-gateway }
  ports:
  - port: 80
    targetPort: 8080
  type: ClusterIP
```

---

## 3. rv-gateway C# ����
...
(����, ���� markdown �״��� ����)
