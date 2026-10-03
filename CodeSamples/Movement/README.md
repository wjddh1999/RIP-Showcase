# Networked Movement

네트워크 입력 소비와 일반 이동 계산을 담당하는 개인 구현 코드입니다.

## Files

| 파일 | 역할 |
|---|---|
| [PlayerMovement.cs](./PlayerMovement.cs) | Fusion 입력을 읽고 카메라 기준 이동·부스트 방향을 컨트롤러로 전달 |
| [PlayerMovementMotor.cs](./PlayerMovementMotor.cs) | 일반 이동의 수직·수평 속도, 에너지 소비·회전·해제 감속 |

## Execution Flow

```text
Input Provider → Fusion Input → PlayerMovement.FixedUpdateNetwork
  → Controller의 경로 선택
  → Melee Dash / Quick Boost / Assault Boost / Normal Movement
```

컨트롤러는 처리된 이동 경로에서 반환해 같은 Tick에 다른 경로가 이어서 실행되지 않도록 제어합니다. 일반 이동 Motor는 계산한 속도를 CharacterController에 적용합니다.

- 상태는 PlayerControllerData, 튜닝 값은 PlayerMovementStats로 전달합니다.
- 지상·공중 입력에서 카메라 pitch 적용을 구분합니다.
- Quick Boost 방향은 대각선 입력의 우세 축을 사용합니다.
- 일반 이동에서 접지·점프·상승·부스트 해제 감속과 회전 우선순위를 계산합니다.

## Dependencies

컨트롤러·Boost Motor·상태 데이터·설정·에너지·프레젠테이션은 원본 프로젝트에 있습니다. 이 선별본은 단독 실행되지 않습니다.
