# Combat Damage Pipeline

팀 공동 전투 시스템의 요청·계산·결과 구조입니다. 개인 기여는 매치 피해 배율 연결과 로컬 피격 피드백 통합이며 ACS·전투 계산과 무기 확장에는 팀원의 구현이 포함됩니다.

## Files

| 파일 | 역할 |
|---|---|
| [DamageRequest.cs](./DamageRequest.cs) | 공격자·피해량·충격량·공격 ID 요청 |
| [DamageResolver.cs](./DamageResolver.cs) | Authority에서 필드·Shield·Armor·ACS 상태 반영 |
| [DamageResolved.cs](./DamageResolved.cs) | AP·ACS 전후 값과 처리 결과 |
| [IDamageReceiver.cs](./IDamageReceiver.cs) | 공격 판정 계층의 요청 전달 |
| [DamageType.cs](./DamageType.cs) | 물리·에너지·폭발 피해 분류 |

## Resolution Order

1. State Authority를 확인하고 피해량·충격량을 0 이상으로 보정합니다.
2. 매치 피해 배율을 적용하고 무효화 필드를 개인 Shield보다 먼저 판정합니다.
3. Shield 흡수량을 분리하고 남은 피해에 방어 타입별 Armor 감소를 적용합니다.
4. ACS Overload 상태의 AP 피해 배율을 적용합니다.
5. AP·ACS를 반영하고 전후 값을 결과로 반환합니다.

AttackId는 공격 소스·공격 객체·Simulation Tick으로 생성하는 식별값입니다. 실제 수치 반영은 Authority에 두고 결과 데이터와 피격 알림을 표현 계층에 전달합니다.

## Dependencies

UnitCombatState, MultiplayerMatchManager와 Photon Fusion은 원본 프로젝트에 있습니다. 피격 피드백은 프로젝트의 State Authority → Input Authority RPC와 UI 이벤트로 연결되며 해당 UI 코드는 포함하지 않습니다. 샘플은 단독 실행되지 않습니다.
