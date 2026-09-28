# Unity 에셋 제작·관리 파이프라인

R.I.P의 교체형 메카 파츠와 무기를 게임 데이터로 연결하기 위해 만든 Unity Editor 도구입니다. 모델에서 프리팹과 ScriptableObject를 생성·등록하는 창과, 등록된 데이터를 검색·검증·편집하는 창으로 구성됩니다. R.I.P 전용 데이터 구조에 맞춘 구현이며 범용 Unity 패키지는 아닙니다.

## 작업 흐름

```text
FBX 모델 / 무기 프리팹
  → ID·장착 본·발사 지점 등 설정 및 검사
  → 게임용 프리팹 + ScriptableObject 생성
  → FramePartDatabaseSO / WeaponDatabase 등록
  → 종류별 검색·검증·Inspector 편집
```

## FBX Gameplay Asset Creator

`RIP/FBX Gameplay Asset Creator`와 `RIP/Prefab Gameplay Asset Creator` 메뉴로 엽니다. 프레임 파츠는 FBX, 무기는 FBX 또는 기존 프리팹을 입력으로 받습니다. ID, 장착 본, 무기 발사 지점, 부스트 포인트, 출력 폴더, 대상 데이터베이스를 설정하고 `Validate & Apply`를 실행합니다.

- 프레임 파츠: 상·하체 설정을 바탕으로 프리팹과 `FramePartSO`를 만듭니다.
- 무기: 장착 기준점과 호환 슬롯 등을 설정해 프리팹과 `WeaponSO`를 만듭니다.
- 필수 입력, 중복 ID, 출력 파일 충돌을 검사합니다. 생성 도중 실패하면 새로 만든 SO와 프리팹을 정리합니다.
- 생성한 SO는 해당 데이터베이스에 등록합니다.

실제 소스의 DB 등록 로직을 간추린 코드:

```csharp
private static void AppendToDatabase(
    UnityEngine.Object database,
    string arrayPropertyName,
    UnityEngine.Object asset)
{
    Undo.RecordObject(database, "Register generated gameplay asset");
    SerializedObject serialized = new SerializedObject(database);
    SerializedProperty array = serialized.FindProperty(arrayPropertyName);
    if (array == null || !array.isArray)
    {
        throw new InvalidOperationException(
            $"Database property '{arrayPropertyName}' was not found.");
    }

    for (int i = 0; i < array.arraySize; i++)
    {
        if (array.GetArrayElementAtIndex(i).objectReferenceValue == asset)
            return;
    }

    int newIndex = array.arraySize;
    array.InsertArrayElementAtIndex(newIndex);
    array.GetArrayElementAtIndex(newIndex).objectReferenceValue = asset;
    serialized.ApplyModifiedProperties();
    EditorUtility.SetDirty(database);
}
```

## Modular Data Manager

`RIP/Tools/Modular Data Manager` 메뉴로 엽니다. DB에 등록된 프레임 상·하체와 무기를 종류별로 표시하고 ID·표시 이름으로 검색합니다. 선택한 SO의 Inspector를 같은 창에서 열어 편집할 수 있습니다.

- 프레임 정의 오류, 비어 있는 무기 ID, 중복 ID, DB의 null 항목을 표시합니다.
- 선택 항목을 Project 창에서 찾고 에셋 변경 내용을 저장할 수 있습니다.
- SO 삭제 시 확인 대화상자에 경로와 기본 파츠 여부를 표시합니다. 삭제 후 DB 등록과 기본 참조를 정리합니다.

실제 소스의 검증 로직을 간추린 코드:

```csharp
private void DrawValidationMessages()
{
    if (_selectedAsset is FramePartSO part)
    {
        if (!part.ValidateDefinition(out string error))
            EditorGUILayout.HelpBox(error, MessageType.Error);

        if (CountFrameId(part.PartId, part.Slot) > 1)
            EditorGUILayout.HelpBox(
                $"Duplicate Part ID '{part.PartId}' exists in {part.Slot}.",
                MessageType.Error);
    }
    else if (_selectedAsset is WeaponSO weapon)
    {
        if (string.IsNullOrWhiteSpace(weapon.weaponId))
            EditorGUILayout.HelpBox("Weapon ID is empty.", MessageType.Error);
        else if (CountWeaponId(weapon.weaponId) > 1)
            EditorGUILayout.HelpBox(
                $"Duplicate Weapon ID '{weapon.weaponId}' exists.",
                MessageType.Error);
    }
}
```

## 코드 근거

| 동작 | 원본 소스의 메서드 |
|---|---|
| 입력과 ID 검사 | `ValidateCommon`, `CreateFramePart`, `CreateWeapon` |
| 프리팹·SO 생성과 DB 등록 | `CreateFramePart`, `CreateWeapon`, `AppendToDatabase` |
| 생성 실패 시 새 파일 정리 | 두 생성 메서드의 `catch` |
| 검색·편집·검증 | `RefreshVisibleAssets`, `DrawSelectedAssetInspector`, `DrawValidationMessages` |
| 확인 후 삭제와 DB 정리 | `TryDeleteSelectedAsset`, `RemoveDatabaseRegistrations` |

코드 예시는 비공개 R.I.P 프로젝트의 자체 Editor 소스에서 가져왔습니다. 전체 Unity 프로젝트와 외부 에셋은 공개하지 않으며, 발췌 코드는 프로젝트 데이터 타입에 의존해 단독 실행되지 않습니다. 작업 시간 절감 수치는 측정하지 않았습니다.

