using System;
using System.Collections.Generic;
using RIP.Player.Modular;
using RIP.Weapons.Core;
using RIP.Weapons.Database;
using UnityEditor;
using UnityEngine;

// Selected Editor operations; window fields, UI and helper members are omitted.
public sealed class ModularDataManagerWindow : EditorWindow
{
    private void DrawValidationMessages()
    {
        if (_selectedAsset is FramePartSO part)
        {
            if (!part.ValidateDefinition(out string error))
            {
                EditorGUILayout.HelpBox(error, MessageType.Error);
            }

            if (CountFrameId(part.PartId, part.Slot) > 1)
            {
                EditorGUILayout.HelpBox(
                    $"Duplicate Part ID '{part.PartId}' exists in {part.Slot}.",
                    MessageType.Error);
            }
        }
        else if (_selectedAsset is WeaponSO weapon)
        {
            if (string.IsNullOrWhiteSpace(weapon.weaponId))
            {
                EditorGUILayout.HelpBox("Weapon ID is empty.", MessageType.Error);
            }
            else if (CountWeaponId(weapon.weaponId) > 1)
            {
                EditorGUILayout.HelpBox(
                    $"Duplicate Weapon ID '{weapon.weaponId}' exists.",
                    MessageType.Error);
            }
        }
    }

    private bool TryDeleteSelectedAsset()
    {
        UnityEngine.Object asset = _selectedAsset;
        string assetPath = AssetDatabase.GetAssetPath(asset);
        if (asset == null ||
            string.IsNullOrEmpty(assetPath) ||
            !assetPath.StartsWith("Assets/", StringComparison.Ordinal))
        {
            EditorUtility.DisplayDialog(
                "Cannot Delete SO",
                "The selected object is not a deletable project asset.",
                "OK");
            return false;
        }

        bool isFramePart = asset is FramePartSO;
        UnityEngine.Object database = isFramePart
            ? (UnityEngine.Object)_frameDatabase
            : _weaponDatabase;
        string arrayName = isFramePart ? "parts" : "weapons";
        List<int> registrationIndices =
            FindRegistrationIndices(database, arrayName, asset);
        bool isDefaultUpper = false;
        bool isDefaultLower = false;
        if (isFramePart && _frameDatabase != null)
        {
            SerializedObject frameDatabase = new SerializedObject(_frameDatabase);
            isDefaultUpper =
                frameDatabase.FindProperty("defaultUpperBody").objectReferenceValue == asset;
            isDefaultLower =
                frameDatabase.FindProperty("defaultLowerBody").objectReferenceValue == asset;
        }

        string defaultWarning = isDefaultUpper || isDefaultLower
            ? "\n\nThis SO is a default frame part. Its default reference will also be cleared."
            : string.Empty;
        bool confirmed = EditorUtility.DisplayDialog(
            "Delete SO Asset?",
            $"Do you really want to delete '{asset.name}'?\n\n" +
            $"Path: {assetPath}\n\n" +
            "The asset will be permanently deleted and removed from its database. " +
            "This action cannot be undone." +
            defaultWarning,
            "Delete",
            "Cancel");
        if (!confirmed)
        {
            return false;
        }

        DestroyCachedEditor();
        if (!AssetDatabase.DeleteAsset(assetPath))
        {
            EditorUtility.DisplayDialog(
                "Delete Failed",
                $"Unity could not delete the asset at:\n{assetPath}",
                "OK");
            return false;
        }

        RemoveDatabaseRegistrations(
            database,
            arrayName,
            registrationIndices,
            isDefaultUpper,
            isDefaultLower);
        _selectedAsset = null;
        AssetDatabase.SaveAssets();
        Repaint();
        return true;
    }

    private static void RemoveDatabaseRegistrations(
        UnityEngine.Object databaseAsset,
        string arrayName,
        List<int> registrationIndices,
        bool clearDefaultUpper,
        bool clearDefaultLower)
    {
        if (databaseAsset == null)
        {
            return;
        }

        SerializedObject database = new SerializedObject(databaseAsset);
        SerializedProperty array = database.FindProperty(arrayName);
        if (array != null && array.isArray)
        {
            for (int i = registrationIndices.Count - 1; i >= 0; i--)
            {
                int index = registrationIndices[i];
                if (index < array.arraySize)
                {
                    int previousSize = array.arraySize;
                    array.DeleteArrayElementAtIndex(index);
                    if (array.arraySize == previousSize)
                    {
                        array.DeleteArrayElementAtIndex(index);
                    }
                }
            }
        }

        if (clearDefaultUpper)
        {
            database.FindProperty("defaultUpperBody").objectReferenceValue = null;
        }

        if (clearDefaultLower)
        {
            database.FindProperty("defaultLowerBody").objectReferenceValue = null;
        }

        database.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(databaseAsset);
    }

}
