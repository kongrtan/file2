# Dapr External Components 정리

이 문서는 Dapr에서 **External Components** 개념과 관련 공식 문서를 요약한 것입니다.  
현재 Dapr에서는 모든 컴포넌트를 런타임에 동적으로 플러그인처럼 로드하는 대신,  
**내장 컴포넌트**(Go 기반 `components-contrib`)와 **외부 gRPC 컴포넌트** 두 가지 방식을 지원합니다.

---

## 1. External Components 개념

- Dapr은 컴포넌트를 통해 상태 저장소, Pub/Sub, 바인딩, 시크릿 스토어 등을 확장할 수 있음
- 일반적으로는 Dapr runtime(`daprd`) 안에 Go로 작성된 컴포넌트가 포함되어 있음
- 하지만 특정 카테고리(예: **bindings**, **state stores**, **secret stores**)는 **외부 gRPC 컴포넌트**로 구현 가능
- 외부 컴포넌트는 독립 실행형 프로세스 또는 컨테이너로 실행되며, Dapr sidecar가 gRPC로 연결

---

## 2. 지원 범위

✅ 외부 gRPC 컴포넌트로 구현 가능한 것들:
- **State Store**
- **Secret Store**
- **Input/Output Bindings**

❌ 아직 공식적으로 지원되지 않는 것:
- **Pub/Sub**  
  (→ 현재는 Go 기반 `components-contrib`에 추가 빌드해야만 동작)

---

## 3. 컴포넌트 스펙 (YAML Schema)

모든 컴포넌트는 공통적으로 다음 스키마를 가짐:

```yaml
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: <컴포넌트 이름>
spec:
  type: <컴포넌트 타입>
  version: v1
  metadata:
  - name: <키>
    value: <값>
```

- `type` → 컴포넌트 종류 (예: `state.redis`, `bindings.kafka`, `secretstores.local.file`)
- `metadata` → 연결 정보, 인증 정보 등

외부 gRPC 컴포넌트를 사용할 경우 `metadata` 안에 `endpoint`(호스트:포트) 정보를 반드시 포함해야 함.

---

## 4. 공식 문서 링크

- [Component Spec (YAML Schema)](https://docs.dapr.io/reference/resource-specs/component-schema/)
- [Components Concept](https://docs.dapr.io/concepts/components-concept/)
- [Supported Components Reference](https://docs.dapr.io/reference/components-reference/)

---

## 5. 요약

- Dapr은 외부 gRPC 컴포넌트를 통해 Go에 종속되지 않고 확장 가능
- 단, **Pub/Sub는 현재 external gRPC 방식을 지원하지 않음**
- 따라서 Pub/Sub을 커스텀하려면 **Go 기반으로 `components-contrib`에 추가 빌드**해야 함
- .NET/Java/Python 등으로 외부 컴포넌트를 만들려면 **state, secret, bindings** 계열로 접근하는 것이 현재 가능한 방법
