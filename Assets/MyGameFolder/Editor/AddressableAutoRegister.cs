using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using System.IO;
using System;
using System.Collections.Generic;

#if UNITY_EDITOR

namespace JPS.Editor
{
    public class AddressableAutoRegister : AssetPostprocessor
    {
        [MenuItem("Tools/Addressables/一括自動登録")]
        public static void RegisterAllAddressables()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("AddressableAssetSettingsが見つかりません");
                return;
            }
            var assetPaths = Directory.GetFiles(AUTO_ADDRESSABLE_FOLDER, "*.*", SearchOption.AllDirectories);
            int count = 0;
            foreach (var assetPath in assetPaths)
            {
                var normPath = assetPath.Replace(Path.DirectorySeparatorChar, '/');
                if (AssetDatabase.IsValidFolder(normPath) || normPath.EndsWith(".meta")) continue;
                string ext = Path.GetExtension(normPath).ToLower();
                if (Array.IndexOf(validExts, ext) < 0) continue;
                var entry = settings.FindAssetEntry(normPath);
                if (entry != null) continue;
                var relPath = normPath.Substring(AUTO_ADDRESSABLE_FOLDER.Length + 1);
                string groupName = "Default";
                int slashIdx = relPath.IndexOf('/');
                if (slashIdx > 0)
                {
                    groupName = relPath.Substring(0, slashIdx);
                }
                var group = settings.FindGroup(groupName);
                if (group == null)
                {
                    // デフォルトのスキーマを追加してグループ作成
                    var schemas = new List<UnityEditor.AddressableAssets.Settings.AddressableAssetGroupSchema>();
                    var bundledSchema = group == null ? ScriptableObject.CreateInstance<UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema>() : null;
                    var contentUpdateSchema = group == null ? ScriptableObject.CreateInstance<UnityEditor.AddressableAssets.Settings.GroupSchemas.ContentUpdateGroupSchema>() : null;
                    if (bundledSchema != null) schemas.Add(bundledSchema);
                    if (contentUpdateSchema != null) schemas.Add(contentUpdateSchema);
                    group = settings.CreateGroup(groupName, false, false, false, schemas, null);
                }
                else
                {
                    // 既存グループにスキーマがなければ追加
                    var bundledSchema = group.GetSchema<UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema>();
                    if (bundledSchema == null)
                    {
                        bundledSchema = ScriptableObject.CreateInstance<UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema>();
                        group.AddSchema(bundledSchema);
                    }
                    var contentUpdateSchema = group.GetSchema<UnityEditor.AddressableAssets.Settings.GroupSchemas.ContentUpdateGroupSchema>();
                    if (contentUpdateSchema == null)
                    {
                        contentUpdateSchema = ScriptableObject.CreateInstance<UnityEditor.AddressableAssets.Settings.GroupSchemas.ContentUpdateGroupSchema>();
                        group.AddSchema(contentUpdateSchema);
                    }
                }
                entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(normPath), group);
                // シーンファイルの場合はファイル名（拡張子なし）だけをアドレスに
                if (ext == ".unity")
                {
                    entry.address = Path.GetFileNameWithoutExtension(normPath);
                }
                else
                {
                    entry.address = Path.ChangeExtension(relPath, null);
                }
                EditorUtility.SetDirty(group);
                count++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[Auto Addressable] 一括登録完了: {count}件");
        }

        private const string AUTO_ADDRESSABLE_FOLDER = "Assets/AddressableAssets";
        private static readonly string[] validExts = { ".prefab", ".png", ".jpg", ".jpeg", ".bmp", ".tga", ".gif", ".psd", ".tiff", ".exr", ".hdr", ".unity" };

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (string assetPath in importedAssets)
            {
                TrySetAddressable(assetPath);
            }

            for (int i = 0; i < movedAssets.Length; i++)
            {
                TrySetAddressable(movedAssets[i]);
                TryRemoveAddressable(movedFromAssetPaths[i]);
            }
        }

        private static void TrySetAddressable(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath) || assetPath.EndsWith(".meta")) return;
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return;
            if (assetPath.StartsWith(AUTO_ADDRESSABLE_FOLDER))
            {
                string ext = Path.GetExtension(assetPath).ToLower();
                if (Array.IndexOf(validExts, ext) < 0) return;
                var entry = settings.FindAssetEntry(assetPath);
                if (entry != null) return;
                // AddressableAssets/ 以降のパス
                var relPath = assetPath.Substring(AUTO_ADDRESSABLE_FOLDER.Length + 1); // +1 for '/'
                // 一つ上の階層名を group名に
                string groupName = "Default";
                int slashIdx = relPath.IndexOf('/');
                if (slashIdx > 0)
                {
                    groupName = relPath.Substring(0, slashIdx);
                }
                // group取得 or 作成（スキーマも考慮）
                var group = settings.FindGroup(groupName);
                if (group == null)
                {
                    var schemas = new List<UnityEditor.AddressableAssets.Settings.AddressableAssetGroupSchema>();
                    var bundledSchema = ScriptableObject.CreateInstance<UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema>();
                    var contentUpdateSchema = ScriptableObject.CreateInstance<UnityEditor.AddressableAssets.Settings.GroupSchemas.ContentUpdateGroupSchema>();
                    schemas.Add(bundledSchema);
                    schemas.Add(contentUpdateSchema);
                    group = settings.CreateGroup(groupName, false, false, false, schemas, null);
                }
                else
                {
                    var bundledSchema = group.GetSchema<UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema>();
                    if (bundledSchema == null)
                    {
                        bundledSchema = ScriptableObject.CreateInstance<UnityEditor.AddressableAssets.Settings.GroupSchemas.BundledAssetGroupSchema>();
                        group.AddSchema(bundledSchema);
                    }
                    var contentUpdateSchema = group.GetSchema<UnityEditor.AddressableAssets.Settings.GroupSchemas.ContentUpdateGroupSchema>();
                    if (contentUpdateSchema == null)
                    {
                        contentUpdateSchema = ScriptableObject.CreateInstance<UnityEditor.AddressableAssets.Settings.GroupSchemas.ContentUpdateGroupSchema>();
                        group.AddSchema(contentUpdateSchema);
                    }
                }
                entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(assetPath), group);
                // シーンファイルの場合はファイル名（拡張子なし）だけをアドレスに
                if (ext == ".unity")
                {
                    entry.address = Path.GetFileNameWithoutExtension(assetPath);
                }
                else
                {
                    entry.address = Path.ChangeExtension(relPath, null);
                }
                EditorUtility.SetDirty(group);
                Debug.Log($"[Auto Addressable] Added: {entry.address} (Group: {groupName})");
            }
            else
            {
                TryRemoveAddressable(assetPath);
            }
        }

        private static void TryRemoveAddressable(string assetPath)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return;
            var entry = settings.FindAssetEntry(assetPath);
            if (entry != null)
            {
                settings.RemoveAssetEntry(entry.guid);
                Debug.Log($"[Auto Addressable] Removed: {assetPath}");
            }
        }
    }
}

#endif