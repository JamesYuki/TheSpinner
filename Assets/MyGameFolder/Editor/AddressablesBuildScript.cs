// using UnityEditor;
// using UnityEditor.AddressableAssets;
// using UnityEditor.AddressableAssets.Build;
// using UnityEditor.AddressableAssets.Settings;

// #if UNITY_EDITOR

// public class AddressablesBuildScript
// {
//     [MenuItem("Tools/Addressables/ビルド実行（手動）")]
//     public static void BuildAddressablesManually()
//     {
//         UnityEngine.Debug.Log("Addressablesコンテンツのビルドを開始します...");
//         var settings = AddressableAssetSettingsDefaultObject.Settings;
//         if (settings == null)
//         {
//             UnityEngine.Debug.LogError("AddressableAssetSettingsが見つかりません。");
//             return;
//         }
//         // 現在のビルドシーン設定を保存
//         var originalScenes = EditorBuildSettings.scenes;
//         try
//         {
//             EditorUtility.DisplayProgressBar("Addressablesビルド", "Addressablesコンテンツのビルド中...", 0.5f);
//             AddressableAssetSettings.BuildPlayerContent();
//             EditorUtility.ClearProgressBar();
//             UnityEngine.Debug.Log("Addressablesコンテンツのビルドが完了しました。");
//         }
//         catch (System.Exception ex)
//         {
//             EditorUtility.ClearProgressBar();
//             UnityEngine.Debug.LogError($"Addressablesビルド中に例外が発生しました: {ex.Message}\n{ex.StackTrace}");
//         }
//         finally
//         {
//             // ビルドシーン設定を復元
//             EditorBuildSettings.scenes = originalScenes;
//         }
//     }

//     public class AutoBuildOnPlayerBuild : UnityEditor.Build.IPreprocessBuildWithReport
//     {
//         public int callbackOrder { get { return 0; } }
//         public void OnPreprocessBuild(UnityEditor.Build.Reporting.BuildReport report)
//         {
//             UnityEngine.Debug.Log("Playerビルド開始前: Addressablesのコンテンツを自動ビルドします。");
//             BuildAddressablesManually();
//         }
//     }
// }

// #endif