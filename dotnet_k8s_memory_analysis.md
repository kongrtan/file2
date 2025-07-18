
# .NET 8 + Kubernetes 메모리 이슈 분석 가이드 (Prometheus 기반)

## 📌 상황 요약

- 실행 환경: .NET 8 런타임 컨테이너
- 실행 방식: `dotnet aa.dll`로 웹서비스 기동
- Kubernetes 설정:
  - `memory limit = 3Gi`
  - 동일 스펙 Pod 2개 기동
- 현상:
  - Pod A: 메모리 사용량 2.5Gi
  - Pod B: 메모리 사용량 500Mi
- 트래픽: 둘 다 거의 없음
- 문제: Pod A의 메모리 사용량이 줄지 않음 (GC 미작동 혹은 누수 의심)

---

## 🔎 원인 후보

| 가능성 | 설명 |
|--------|------|
| ✅ 정상적인 GC 동작 | .NET GC는 사용 후 메모리를 OS에 반환하지 않음 |
| ⚠️ GC 작동 약함 | Long-lived 객체 많으면 Gen2 수집 안 됨 |
| 🔺 메모리 누수 | Singleton, static, cache, event 누락 등 |

---

## ✅ Prometheus 메트릭 비교 항목

| 메트릭 | 의미 | 사용 목적 |
|--------|------|-----------|
| `dotnet_totalmemory_bytes` | GC Heap의 사용 중인 managed 메모리 | 핵심 지표 |
| `process_working_set_bytes` | OS에서 실제 점유한 물리 메모리 | 운영 메모리 사용량 |
| `process_private_memory_bytes` | .NET 프로세스가 확보한 전체 메모리 | 메모리 누적 경향 분석 |
| `dotnet_collection_count{generation="2"}` | Gen2 GC 발생 횟수 | Long-lived 객체 유무 분석 |
| `dotnet_collection_count{generation="0"}` | Gen0 GC 발생 횟수 | 단기 할당량 분석 |
| `dotnet_gc_allocated_bytes_total` | 누적 할당된 GC memory 크기 | 전체 객체 생성량 추정 |
| `dotnet_threadpool_threads_count` | 쓰레드풀의 크기 | Thread → Memory 간접 영향 분석 |

---

## 📊 실전 PromQL 예제

### 1. 메모리 사용량 비교

```promql
dotnet_totalmemory_bytes{pod="aa-1"}
dotnet_totalmemory_bytes{pod="aa-2"}
```

### 2. GC 수 비교 (10분 기준)

```promql
increase(dotnet_collection_count{pod="aa-1",generation="2"}[10m])
increase(dotnet_collection_count{pod="aa-2",generation="2"}[10m])
```

### 3. 프로세스 메모리 분석

```promql
process_working_set_bytes{pod=~"aa-.*"}
process_private_memory_bytes{pod=~"aa-.*"}
```

---

## 🧠 해석 가이드

| 증상 | 의심 원인 |
|------|-----------|
| GC Heap 크기 고정, GC 횟수 적음 | Long-lived 객체 또는 GC가 느림 |
| Working Set 유지, 트래픽 없음 | unmanaged 메모리 누수 or GC 미작동 |
| Allocated Bytes 증가 지속 | 객체 과다 생성 or 불필요한 보관 |

---

## ✅ 권장 조치 순서

1. `dotnet-counters`로 GC 상태 점검
2. `dotnet-gcdump`로 memory 덤프 비교 (문제 Pod vs 정상 Pod)
3. Prometheus로 시계열 분석하여 GC 작동/누수 여부 판단
4. 필요 시 메모리 캐시, static field, 이벤트 등록 누락 확인

---

## 🛠 관련 도구

| 도구 | 설명 |
|------|------|
| `dotnet-counters` | GC/Heap 상태 실시간 확인 |
| `dotnet-gcdump` | GC 대상 객체 힙 덤프 수집 |
| `dotnet-dump` | 프로세스 전체 메모리 분석 |
| `prometheus-net` | .NET 앱 내 Prometheus exporter |
| `Grafana` | 메트릭 시각화 및 비교 대시보드 구성 |

---

## 📎 참고 팁

- `dotnet_totalmemory_bytes`는 **K8s 리소스 limit과 무관**
- .NET GC는 heap을 OS에 자동 반환하지 않음
- 메모리 누수가 의심되면 dump 분석이 가장 정확함

---

## 🧰 필요시 제공 가능

- Prometheus + Grafana 대시보드 JSON 템플릿
- dotnet-gcdump 비교 분석 스크립트
- Singleton, Cache, DI 구조 메모리 점검 체크리스트
