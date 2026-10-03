using System;
using System.Collections.Generic;
using System.IO;
using Fusion;
using RIP.Player.Modular;
using RIP.Weapons.Core;
using RIP.Weapons.Database;
using RIP.Weapons.Fire;
using UnityEditor;
using UnityEngine;

// Selected Editor operations; window fields, UI and helper members are omitted.
public sealed class FbxGameplayAssetCreatorWindow : EditorWindow
{
    private void ValidateCommon()
    {
        if (_sourceModel == null || string.IsNullOrEmpty(_sourcePath))
        {
            throw new InvalidOperationException("Select a source asset.");
        }

        bool isFbx = IsFbxAssetPath(_sourcePath);
        bool isWeaponPrefab = _category == AssetCategory.Weapon &&
                              _sourceAllowsPrefab &&
                              IsPrefabAssetPath(_sourcePath);

        if (_category == AssetCategory.FramePart && !isFbx)
        {
            throw new InvalidOperationException("FramePart creation requires an FBX model asset.");
        }

        if (_category == AssetCategory.Weapon && !isFbx && !isWeaponPrefab)
        {
            throw new InvalidOperationException("Weapon creation requires an FBX model asset or a prefab source.");
        }

        if (string.IsNullOrWhiteSpace(_assetId) || _assetId.Length > 64)
        {
            throw new InvalidOperationException("The asset ID must contain 1 to 64 characters.");
        }

        if (string.IsNullOrWhiteSpace(_assetName) ||
            _assetName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new InvalidOperationException("Enter a valid asset file name.");
        }

        if (string.IsNullOrWhiteSpace(_displayName))
        {
            throw new InvalidOperationException("Display Name is required.");
        }
    }

    private void CreateFramePart()
    {
        if (_frameTemplate != null && _frameTemplate.Slot != _frameSlot)
        {
            throw new InvalidOperationException(
                "Select a FramePartSO stats template with the same part type.");
        }

        if (_frameDatabase == null)
        {
            throw new InvalidOperationException("Select a FramePartDatabaseSO.");
        }

        string assemblyPath = RequirePath(_assemblyBoneIndex, "Assembly Bone");
        string rightHandPath = null;
        string leftHandPath = null;
        string rightShoulderPath = null;
        string leftShoulderPath = null;
        if (_frameSlot == FramePartSlot.UpperBody)
        {
            rightHandPath = RequirePath(_rightHandIndex, "Right Hand bone");
            leftHandPath = RequirePath(_leftHandIndex, "Left Hand bone");
            rightShoulderPath = RequirePath(_rightShoulderIndex, "Right Shoulder bone");
            leftShoulderPath = RequirePath(_leftShoulderIndex, "Left Shoulder bone");
        }
        LowerBodyRigCreationSettings lowerBodyRig =
            BuildLowerBodyRigCreationSettings();

        if (_frameDatabase.GetPartById(_assetId, _frameSlot) != null)
        {
            throw new InvalidOperationException(
                $"Part ID '{_assetId}' already exists in the selected database.");
        }

        ValidateFolder(_framePrefabFolder);
        ValidateFolder(_frameDataFolder);
        string prefabPath = CombineAssetPath(
            _framePrefabFolder,
            _assetName + ".prefab");
        string dataPath = CombineAssetPath(
            _frameDataFolder,
            _assetName + ".asset");
        ValidateNewPaths(prefabPath, dataPath);

        EnsureAssetFolder(_framePrefabFolder);
        EnsureAssetFolder(_frameDataFolder);
        bool prefabCreated = false;
        bool dataCreated = false;
        try
        {
            BoostPointPathSet boostPoints = new BoostPointPathSet(
                _rearBoostPointPaths,
                _frontBoostPointPaths,
                _leftBoostPointPaths,
                _rightBoostPointPaths,
                _footBoostPointPaths);
            GameObject prefab = CreateWrapperPrefab(
                prefabPath,
                false,
                null,
                boostPoints);
            prefabCreated = true;

            ConfigureFrameAuthoring(
                prefabPath,
                _frameSlot,
                assemblyPath,
                rightHandPath,
                leftHandPath,
                rightShoulderPath,
                leftShoulderPath,
                boostPoints,
                lowerBodyRig);
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            FramePartSO part = CreateInstance<FramePartSO>();
            if (_frameTemplate != null)
            {
                EditorUtility.CopySerialized(_frameTemplate, part);
            }

            SerializedObject serialized = new SerializedObject(part);
            if (_frameTemplate == null)
            {
                ApplyDefaultFrameStats(serialized, _frameSlot);
            }

            serialized.FindProperty("partId").stringValue = _assetId;
            serialized.FindProperty("displayName").stringValue = _displayName;
            serialized.FindProperty("slot").enumValueIndex = (int)_frameSlot;
            serialized.FindProperty("visualPrefab").objectReferenceValue = prefab;
            serialized.FindProperty("lowerBodyType").enumValueIndex =
                (int)lowerBodyRig.Type;

            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(part, dataPath);
            dataCreated = true;
            if (!part.ValidateDefinition(out string error))
            {
                throw new InvalidOperationException(error);
            }

            AppendToDatabase(_frameDatabase, "parts", part);
            AssetDatabase.SaveAssets();
            Selection.activeObject = part;
        }
        catch
        {
            if (dataCreated)
            {
                AssetDatabase.DeleteAsset(dataPath);
            }

            if (prefabCreated)
            {
                AssetDatabase.DeleteAsset(prefabPath);
            }

            throw;
        }
    }

    private void CreateWeapon()
    {
        if (_weaponDatabase == null)
        {
            throw new InvalidOperationException("Select a WeaponDatabase.");
        }

        if (_compatibleSlots == WeaponSlotMask.None)
        {
            _compatibleSlots = GetDefaultWeaponSlots(_weaponType);
        }

        string mountPath = RequirePath(_mountBoneIndex, "Mount Reference");
        if (_weaponType != WeaponType.Melee &&
            _weaponType != WeaponType.Shield &&
            _selectedFirePointPaths.Count == 0)
        {
            throw new InvalidOperationException(
                "A non-melee weapon requires at least one Fire Point.");
        }

        if (_weaponDatabase.GetWeaponById(_assetId) != null)
        {
            throw new InvalidOperationException(
                $"Weapon ID '{_assetId}' already exists in the selected database.");
        }

        ValidateFolder(_weaponPrefabFolder);
        ValidateFolder(_weaponDataFolder);
        string prefabPath = CombineAssetPath(
            _weaponPrefabFolder,
            _assetName + ".prefab");
        string dataPath = CombineAssetPath(
            _weaponDataFolder,
            _assetName + ".asset");
        ValidateNewPaths(prefabPath, dataPath);

        EnsureAssetFolder(_weaponPrefabFolder);
        EnsureAssetFolder(_weaponDataFolder);
        bool prefabCreated = false;
        bool dataCreated = false;
        try
        {
            GameObject prefab = CreateWrapperPrefab(
                prefabPath,
                true,
                _selectedFirePointPaths,
                null);
            prefabCreated = true;
            Transform modelRoot = GetSavedModelRoot(prefab);
            Transform mountBone = RequireSavedTransform(modelRoot, mountPath);

            WeaponSO weapon = CreateInstance<WeaponSO>();
            if (_weaponTemplate != null)
            {
                EditorUtility.CopySerialized(_weaponTemplate, weapon);
            }

            SerializedObject serialized = new SerializedObject(weapon);
            if (_weaponTemplate == null)
            {
                ApplyDefaultWeaponValues(serialized, _weaponType);
            }

            serialized.FindProperty("weaponId").stringValue = _assetId;
            serialized.FindProperty("displayName").stringValue = _displayName;
            serialized.FindProperty("weaponType").enumValueIndex = (int)_weaponType;
            serialized.FindProperty("compatibleSlots").intValue = (int)_compatibleSlots;
            SerializedProperty presentation = serialized.FindProperty("_presentationConfig");
            presentation.FindPropertyRelative("visualId").stringValue = _assetId;
            presentation.FindPropertyRelative("visualPrefab").objectReferenceValue = prefab;
            presentation.FindPropertyRelative("mountReferenceBone").objectReferenceValue = mountBone;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            weapon.EnsureSerializedConfigMigration();
            AssetDatabase.CreateAsset(weapon, dataPath);
            dataCreated = true;

            AppendToDatabase(_weaponDatabase, "weapons", weapon);
            AssetDatabase.SaveAssets();
            Selection.activeObject = weapon;
        }
        catch
        {
            if (dataCreated)
            {
                AssetDatabase.DeleteAsset(dataPath);
            }

            if (prefabCreated)
            {
                AssetDatabase.DeleteAsset(prefabPath);
            }

            throw;
        }
    }

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
            {
                return;
            }
        }

        int newIndex = array.arraySize;
        array.InsertArrayElementAtIndex(newIndex);
        array.GetArrayElementAtIndex(newIndex).objectReferenceValue = asset;
        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(database);
    }

}
