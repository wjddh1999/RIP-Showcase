# Match Flow

개인 담당 네트워크 흐름 중 세션 잠금·씬 진입·Server 전용 Spawn을 보여줍니다.

## Files

| 파일 | 역할 |
|---|---|
| [NetworkMatchFlow.cs](./NetworkMatchFlow.cs) | 전투 시작 인원 조건, 세션 잠금, 싱글·멀티 씬 진입과 플레이어 생성 |

## Responsibilities

- CanStartBattle는 Runner 실행 상태·세션·Server·최소 참가 인원을 확인합니다. 로비 Ready 조건은 NetworkManager에서 결합합니다.
- 시작 시 세션 입장을 닫고 검색 목록에서 숨깁니다.
- Player Scene은 Single, 선택 Map Scene은 Additive로 로드합니다.
- 씬 준비와 로드아웃을 연결해 Server가 플레이어를 생성합니다.
- 싱글 플레이는 별도 Map 진입 경로를 사용하며 세션 종료 후 흐름을 초기화합니다.

## Related Systems

참가자별 시각 준비와 인트로 시작은 [Battle Readiness](../BattleReadiness/README.md)에서 확인할 수 있습니다. 제한 시간·사망·FFA 탈락·결과는 원본 MatchManager가 담당합니다.

## Dependencies

NetworkManager, PlayerSpawner, MultiplayerMatchManager, MapSceneContext, 로딩 요청과 Photon Fusion 씬 관리가 필요합니다. 이 선별본은 단독 실행되지 않습니다.
