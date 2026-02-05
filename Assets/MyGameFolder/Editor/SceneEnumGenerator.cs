using System.IO;
using System.Linq;
using UnityEditor;

namespace JPS.System.Editor
{
    public static class SceneEnumGenerator
    {
        private const string SceneFolderPath = "Assets/AddressableAssets/Scene";
        private const string OutputPath = "Assets/MyGameFolder/Scripts/System/SceneEnum.cs";

        [MenuItem("Tools/Generate SceneEnum")] 
        public static void Generate()
        {
            var sceneFiles = Directory.GetFiles(SceneFolderPath, "*.unity")
                .Select(Path.GetFileNameWithoutExtension)
                .Where(name => !string.IsNullOrEmpty(name))
                .ToList();

            var enumEntries = sceneFiles.Select(name => $"        {name},");
            var enumBody = string.Join("\n", enumEntries);

            var code = $@"namespace JPS.System
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
            UnityEngine.Debug.Log($"SceneEnum.cs を自動生成しました: {OutputPath}");
        }
    }
}
