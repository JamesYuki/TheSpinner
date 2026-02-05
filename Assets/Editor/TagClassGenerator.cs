using UnityEditor;
using UnityEngine;
using System.IO;
using System.Text;

/// <summary>
/// TagManager.assetのタグをもとにTags.csを自動生成するエディタ拡張。
/// </summary>
[InitializeOnLoad]
public static class TagClassGenerator
{
    private const string OutputPath = "Assets/MyGameFolder/Scripts/General/Tags.cs";
    static TagClassGenerator()
    {
        // Unity起動時とタグ変更時に自動生成
        EditorApplication.delayCall += GenerateTagClass;
        // Projectウィンドウでアセットが変更されたときにも監視
        AssetDatabase.importPackageCompleted += _ => GenerateTagClass();
    }

    [MenuItem("Tools/Generate Tags Class")]
    public static void GenerateTagClass()
    {
        var tags = UnityEditorInternal.InternalEditorUtility.tags;
        var sb = new StringBuilder();
        sb.AppendLine("// 自動生成: Unityタグ定数クラス");
        sb.AppendLine("public static class Tags");
        sb.AppendLine("{");
        foreach (var tag in tags)
        {
            if (!string.IsNullOrEmpty(tag))
            {
                sb.AppendLine($"    public const string {SanitizeTagName(tag)} = \"{tag}\";");
            }
        }
        sb.AppendLine("}");
        try
        {
            var dir = Path.GetDirectoryName(OutputPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(OutputPath, sb.ToString(), Encoding.UTF8);
            AssetDatabase.Refresh();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"TagClassGenerator: failed to write '{OutputPath}': {e}");
        }
    }

    private static string SanitizeTagName(string tag)
    {
        // タグ名をC#の識別子として使えるように変換
        var safe = tag.Replace(" ", "_").Replace("-", "_");
        if (char.IsDigit(safe[0])) safe = "_" + safe;
        return safe;
    }
}
