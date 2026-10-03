# Asset Authoring Tools

프레임 파츠와 무기를 게임 데이터로 연결하는 Unity Editor 도구입니다. 개인 구현을 중심으로 팀원의 무기 관련 확장이 포함됩니다.

## Files

| 파일 | 역할 |
|---|---|
| [FbxGameplayAssetCreatorWindow.cs](./FbxGameplayAssetCreatorWindow.cs) | 공통 입력 검사, 프레임·무기 생성과 DB 등록 메서드 발췌 |
| [ModularDataManagerWindow.cs](./ModularDataManagerWindow.cs) | 선택 에셋 검증, 삭제 확인과 DB·기본 참조 정리 메서드 발췌 |

## Workflow

```text
FBX / Weapon Prefab → Input / ID / Mount Validation
  → Gameplay Prefab + ScriptableObject → Database
  → Search / Validation / Inspector Editing
```

## Creation

RIP/FBX Gameplay Asset Creator와 RIP/Prefab Gameplay Asset Creator 메뉴에서 엽니다. 프레임은 FBX, 무기는 FBX 또는 기존 프리팹을 입력으로 받습니다. ID·장착 본·발사 지점·부스트 포인트·출력 폴더·DB를 설정합니다.

입력·중복 ID·출력 충돌을 확인해 프리팹과 SO를 만들고 DB 배열에 등록합니다. 생성 도중 실패하면 새 SO·프리팹을 정리합니다. DB 등록은 동일 에셋의 중복 참조를 확인하고 Undo·SerializedObject를 사용합니다.

## Management

RIP/Tools/Modular Data Manager는 상·하체·무기를 종류별로 표시하고 ID·표시 이름으로 검색합니다. 같은 창에서 SO Inspector를 편집하고 정의 오류·빈 ID·중복 ID·DB null 항목을 확인합니다.

삭제 확인에는 경로와 기본 파츠 여부를 표시합니다. 삭제 성공 후 DB 등록과 기본 참조를 정리합니다.

## Dependencies

FramePartSO, FramePartDatabaseSO, WeaponSO, WeaponDatabase와 Authoring 보조 코드가 필요합니다. 창 UI·필드·보조 메서드를 생략한 핵심 작업의 발췌본이며 단독 실행되지 않습니다.
