using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.AssetImporters;
using System.IO;
using System.Linq;

namespace Spinner.Editor
{
    public class SceneEnumAutoGenerator : AssetPostprocessor
    {
        private const string SceneFolderPath = "Assets/AddressableAssets/Scene";
        private const string OutputPath = "Assets/MyGameFolder/Scripts/System/SceneEnum.cs";

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            bool sceneChanged = importedAssets.Concat(deletedAssets).Concat(movedAssets).Concat(movedFromAssetPaths)
                .Any(path => path.StartsWith(SceneFolderPath) && path.EndsWith(".unity"));
            if (sceneChanged)
            {
                GenerateSceneEnum();
            }
        }

        public static void GenerateSceneEnum()
        {
            var sceneFiles = Directory.GetFiles(SceneFolderPath, "*.unity")
                .Select(Path.GetFileNameWithoutExtension)
                .Where(name => !string.IsNullOrEmpty(name))
                .ToList();

            var enumEntries = sceneFiles.Select(name => $"        {name},");
            var enumBody = string.Join("\n", enumEntries);

            var code = $@"namespace Spinner
{{
    // このenumはAddressableAssets/Scene内のシーンファイルから自動生成されています。
    public enum SceneEnum
    {{
        None = 0,
{enumBody}
    }}
}}";

            File.WriteAllText(OutputPath, code);
            AssetDatabase.Refresh();
            UnityEngine.Debug.Log($"[自動] SceneEnum.cs を再生成しました: {OutputPath}");
        }
    }
}
