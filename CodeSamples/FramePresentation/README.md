# Frame Presentation

도색 데이터와 탱크 궤도 표현을 연결하는 개인 구현 코드입니다.

## Files

| 파일 | 역할 |
|---|---|
| [FramePaintColorUtility.cs](./FramePaintColorUtility.cs) | RGBA8 정수 패킹·복원과 기존 기본 색 처리 |
| [PlayerLoadoutData.cs](./PlayerLoadoutData.cs) | 파츠·색·무기 구성과 이전 단일 색 fallback |
| [TrackBeltAnimator.cs](./TrackBeltAnimator.cs) | 전진·회전 속도를 좌우 궤도 UV 오프셋에 반영 |

## Color Data

상·하체 색을 별도 필드에 보관합니다. 파츠 색이 기존 기본값이면 단일 색 필드로 fallback하며 패킹된 기본값은 기본 파란색으로 복원합니다. 네트워크 색 전달은 [Modular Frame](../ModularFrame/README.md)의 NetworkModularPlayer에서 확인할 수 있습니다.

## Track Motion

유효한 네트워크 컨트롤러 속도를 사용하고, 그렇지 않으면 위치 변화에서 속도를 계산합니다. 전진 속도에 회전 각속도와 반쪽 궤도 폭을 반영해 좌우 속도를 다르게 만들고 보간한 속도로 UV 오프셋을 누적합니다.

궤도별 런타임 머티리얼을 생성해 원본 텍스처·속성을 전달하고 해제 시 원래 머티리얼을 복원합니다.

## Dependencies

프레임·무기 데이터 타입, PlayerNetworkController, 좌우 궤도 Renderer와 프로젝트의 RIP/Track Atlas Scroll 셰이더가 필요합니다. 도색 적용 Controller와 모델·셰이더 에셋은 포함하지 않아 단독 실행되지 않습니다.
