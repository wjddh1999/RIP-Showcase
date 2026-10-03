# Modular Frame Assembly

상·하체 정의를 외형·스탯·네트워크 빌드로 연결하는 개인 구현 중심의 프레임 시스템입니다.

## Files

| 파일 | 역할 |
|---|---|
| [FramePartSO.cs](./FramePartSO.cs) | 파츠 종류·프리팹·스탯, Authoring 데이터 구조와 정의 검증 |
| [FramePartDatabaseSO.cs](./FramePartDatabaseSO.cs) | ID·슬롯별 조회와 기본 파츠 선택 |
| [ResolvedFrameStats.cs](./ResolvedFrameStats.cs) | 상·하체 기여 합산과 이동 배율 계산 |
| [FrameAssemblyController.cs](./FrameAssemblyController.cs) | 정의 확인, 조립 본 정렬, 스탯 적용과 교체 이벤트 |
| [NetworkModularPlayer.cs](./NetworkModularPlayer.cs) | 파츠 ID·색·빌드 버전 동기화와 외형 적용 |

## Assembly Flow

```text
Part IDs → Database → Definition Validation
  → Prepare Visuals → Align Assembly Bones
  → Apply Stats → AssemblyChanged
```

새 파츠의 정의와 조립 본을 확인한 후 기존 외형을 교체합니다. 조립 완료 이벤트로 무기·히트박스·부스트 표현이 연결을 갱신합니다. 같은 파츠를 다시 적용할 때는 스탯과 이벤트를 갱신합니다.

NetworkModularPlayer는 파츠 ID·상하체별 RGBA 색·BuildVersion을 복제합니다. 인스턴스는 아직 적용하지 않은 버전에 조립을 시도하고 성공하면 적용 버전을 기록합니다. 누락된 ID에는 DB 기본 파츠 또는 첫 파츠를 사용합니다.

## Dependencies

FramePartAuthoring, 체력·에너지·이동 컴포넌트, 무기 타입·로드아웃과 Photon Fusion이 필요합니다. 모델·프리팹·DB 에셋은 포함하지 않아 샘플만으로 실행되지 않습니다.
