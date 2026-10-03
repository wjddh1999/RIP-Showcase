# Battle Entry Readiness

전투 진입에서 플레이어 준비와 로컬 시각 준비를 나눠 관리하는 개인 구현 코드입니다.

## Files

| 파일 | 역할 |
|---|---|
| [MultiplayerMatchManager.Readiness.cs](./MultiplayerMatchManager.Readiness.cs) | 준비 보고 RPC·참가자 검사·인트로 타이머와 매치 시작 메서드 |
| [MultiplayerMatchUI.Readiness.cs](./MultiplayerMatchUI.Readiness.cs) | 카메라 초기 구도·시각 준비 보고·입력·HUD 인계 |

## Readiness Flow

```text
Player Registration + Local Player / Camera Binding
  → Intro Ready Reports → Visual Preparation
  → Camera Initial Pose → Visual Ready Reports
  → Authority Starts Intro TickTimer → Match Start
```

MatchManager는 활성 참가자가 등록되고 준비 보고를 보냈는지 확인해 시각 준비 단계로 전환합니다. UI는 카메라를 시작 구도에 설정하고 다음 호출에서 시각 준비를 보고합니다. 모든 활성 참가자의 보고가 도착하면 State Authority가 공통 TickTimer를 시작합니다.

연출 중에는 입력·HUD를 제어하고 진행도를 카메라·오버레이에 전달합니다. 매치 시작 시 Authority가 매치 타이머와 피해 적용을 활성화합니다.

## Dependencies

두 파일은 관련 멤버 발췌입니다. 플레이어 등록·씬 준비·타이머 만료를 처리하는 네트워크 Tick·UI 생성과 Update 호출·이탈·결과 처리는 원본 프로젝트에 있습니다. Photon Fusion·카메라·입력·HUD·PlayerHealth에 의존하며 단독 실행되지 않습니다.
