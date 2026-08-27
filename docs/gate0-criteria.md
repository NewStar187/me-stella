\# Gate 0 판정 기준 (측정 전 사전 등록)



\- 등록일: 2026-08-27

\- 원칙: 본 기준은 실측 개시 전에 확정하며, 측정값 확인 후 수정하지 않는다.

\- 판정: 전 항목 Go → Go / 1개 이상 Cond.Go, No-Go 없음 → 조건부 Go / 1개라도 No-Go → No-Go

\- No-Go 시 폴백: PRD v0.3 웹 온리 스택 회귀 (비용 2주)



\## 측정 결과



\### A. 빌드·배포 파이프라인 — Go

\- 2026-08-27 Vercel 배포 성공, 공개 URL 실행 확인

\- https://me-stella.vercel.app/unity/index.html



\### 발견 — 콜드 캐시 첫 로드 지연

\- 최초 접속 시 Build.wasm.unityweb(8.1MB) 전송 약 1.7분 (엣지 캐시 MISS)

\- 캐시 적중 후 동일 파일 1.4\~1.8초 (icn1 엣지 HIT 확인)

\- "첫 방문자 경험" 리스크로 등재, 대응은 Gate 0 판정 후 검토



\### B-1. 데스크톱 로딩 — 약 4초 → 판정: Cond.Go

\- 개선 방안: Vercel Content-Encoding 헤더 + Decompression Fallback OFF 재빌드

\- 개선 시점: Gate 0 스파이크 내 헤더 설정 후 재측정 (판정 갱신 가능)

\- 참고: Network Finish 1.55/1.72/1.76초



\### B-2. 모바일 로딩 — 약 4.5초 → 판정: Go (기준 7초)

\- Galaxy S23+ / Android 16 / Samsung Internet, 엣지 캐시 적중 상태



\### C. 1차 관찰 (최종 판정 아님 — 빈 씬 한계)

\- S23+ 로딩 후 1\~2분 조작: 크래시·튕김·표시 이상 없음

\- iOS Safari 미측정 — 실기 섭외 필요 (C 최종 판정 선행 조건)

\- 빌드 총 용량: 11.43MB (배포 전송량 8.3MB)

\## D. 한글 IME (HTML 오버레이) + 모바일 가상 키보드

측정: 한글 입력·조합·삭제·제출 정상 동작 여부

Go / No-Go (이진 판정)



\## E. API 왕복 지연 (Unity → Vercel → Supabase)

측정: 왕복 20회, P75

Go:       1000ms 이하 (근거: PRD F-05 채점 응답 P75 1초 이내)

Cond.Go:  1500ms 이하 (초과분은 캐싱·쿼리 최적화로 회수 가능하다고 판단)

No-Go:    1500ms 초과

단, Cond.Go 판정 시 티어 3 상태 그래프는 '조건부 채택'을 유지하며,

연속 왕복 누적 지연을 별도 검증하기 전까지 확정하지 않는다.

\## F. 수식 사전 렌더 이미지 표시

측정: 이미지 로드·해상도·레이아웃 정상 여부

Go / No-Go (이진 판정)

\* 티어 3 채택 조건 중 2



\## 측정 결과 (실측 후 기입 — 현재 공란)



\### D. HTML 오버레이 IME — Go 잠정

\- 데스크톱 Chrome 물리 키보드: 한글 입력·조합·삭제·SendMessage 표시 정상

\- 표시: OverlayInputReceiver에서 값 수신·화면 출력 확인

\- 최종 판정 보류: 모바일 가상 키보드 (S23+/iOS Safari) 실측 필요



\### E. API 왕복 지연 — Go

\- 20회 왕복, 성공 20 / 실패 0

\- 최소 424ms / 중앙값 466ms / P75 521.1ms

\- 기준 1000ms 대비 약 52% 사용, 여유 확보

\- 티어 3 상태 그래프 채택 조건 1: 충족

\- 비고: /api/health = Vercel Route Handler + Prisma + Supabase 서울 리전 왕복



\### F. 수식 사전 렌더 이미지 표시 — Go

\- 원본(1128×286) → RawImage 표시 확인, AspectRatioFitter 정상

\- 파이프라인: 웹 셸(/formula-sample.png) → UnityWebRequestTexture → 표시

\- 티어 3 상태 그래프 채택 조건 2: 충족

\- 한계: 이번 측정은 매트플롯립 렌더 프록시 사용. 실제 KaTeX 사전 렌더 + Supabase

&#x20; Storage + CORS는 별도 검증 필요 (콘텐츠 파이프라인 F-11 항목)



\### C. 모바일 브라우저 성능 (1차 → 재관찰 필요)

\- 이번 씬은 UI·API·이미지 포함 실질 관찰 대상. 폰 재측정 후 최종 판정.

