#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using Spinner;

namespace Spinner.Editor
{
    /// <summary>
    /// スピナーシステムのセットアップを補助するエディターウィンドウ
    /// </summary>
    public class SpinnerSetupWindow : EditorWindow
    {
        private const string SETTINGS_PATH = "Assets/MyGameFolder/Settings/SpinnerSettings.asset";

        [MenuItem("Spinner/Setup Window")]
        public static void ShowWindow()
        {
            GetWindow<SpinnerSetupWindow>("Spinner Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("Spinner System Setup", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // SpinnerSettings作成
            if (GUILayout.Button("Create SpinnerSettings Asset"))
            {
                CreateSpinnerSettings();
            }

            EditorGUILayout.Space();

            // プレハブ作成ガイド
            EditorGUILayout.HelpBox(
                "スピナープレイヤーのプレハブ構造:\n" +
                "- SpinnerPlayer (SpinnerController, SpinnerInputHandler, PredictedRigidbody)\n" +
                "  |- Core (コア、中央の〇)\n" +
                "  |- ArmPivot (回転軸)\n" +
                "      |- LeftArm (左アーム ==)\n" +
                "      |- RightArm (右アーム ==)",
                MessageType.Info);

            if (GUILayout.Button("Create Spinner Player Prefab Structure"))
            {
                CreateSpinnerPlayerStructure();
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Create Puck Prefab"))
            {
                CreatePuckPrefab();
            }
        }

        private void CreateSpinnerSettings()
        {
            // ディレクトリ確認
            string directory = Path.GetDirectoryName(SETTINGS_PATH);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 既存チェック
            var existing = AssetDatabase.LoadAssetAtPath<SpinnerSettings>(SETTINGS_PATH);
            if (existing != null)
            {
                EditorUtility.DisplayDialog("Info", "SpinnerSettings already exists!", "OK");
                Selection.activeObject = existing;
                return;
            }

            // 作成
            var settings = ScriptableObject.CreateInstance<SpinnerSettings>();
            AssetDatabase.CreateAsset(settings, SETTINGS_PATH);
            AssetDatabase.SaveAssets();

            Selection.activeObject = settings;
            EditorUtility.DisplayDialog("Success", "SpinnerSettings created at:\n" + SETTINGS_PATH, "OK");
        }

        private void CreateSpinnerPlayerStructure()
        {
            // 親オブジェクト作成
            var root = new GameObject("SpinnerPlayer");

            // Rigidbody設定
            var rb = root.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;

            // コンポーネント追加
            root.AddComponent<SpinnerInputHandler>();
            var controller = root.AddComponent<SpinnerController>();

            // コア作成
            var core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.name = "Core";
            core.transform.SetParent(root.transform);
            core.transform.localPosition = Vector3.zero;
            core.transform.localScale = Vector3.one * 0.6f;

            // ArmPivot作成
            var armPivot = new GameObject("ArmPivot");
            armPivot.transform.SetParent(root.transform);
            armPivot.transform.localPosition = Vector3.zero;

            // 左アーム作成
            var leftArm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftArm.name = "LeftArm";
            leftArm.transform.SetParent(armPivot.transform);
            leftArm.transform.localPosition = new Vector3(-1.5f, 0f, 0f);
            leftArm.transform.localScale = new Vector3(1f, 0.3f, 0.3f);

            // 右アーム作成
            var rightArm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightArm.name = "RightArm";
            rightArm.transform.SetParent(armPivot.transform);
            rightArm.transform.localPosition = new Vector3(1.5f, 0f, 0f);
            rightArm.transform.localScale = new Vector3(1f, 0.3f, 0.3f);

            // SpinnerControllerの参照を設定
            var so = new SerializedObject(controller);
            so.FindProperty("m_ArmPivot").objectReferenceValue = armPivot.transform;
            so.FindProperty("m_Core").objectReferenceValue = core.transform;
            so.FindProperty("m_LeftArmCollider").objectReferenceValue = leftArm.GetComponent<Collider>();
            so.FindProperty("m_RightArmCollider").objectReferenceValue = rightArm.GetComponent<Collider>();

            // SpinnerSettings読み込み
            var settings = AssetDatabase.LoadAssetAtPath<SpinnerSettings>(SETTINGS_PATH);
            if (settings != null)
            {
                so.FindProperty("m_Settings").objectReferenceValue = settings;
            }

            so.ApplyModifiedProperties();

            Selection.activeGameObject = root;
            EditorUtility.DisplayDialog("Success",
                "Spinner Player structure created!\n\n" +
                "Note: PredictedRigidbody is not added automatically.\n" +
                "Please add it manually from PurrNet components.", "OK");
        }

        private void CreatePuckPrefab()
        {
            var puck = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            puck.name = "Puck";
            puck.transform.localScale = new Vector3(0.5f, 0.1f, 0.5f);

            // Rigidbody設定
            var rb = puck.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            puck.AddComponent<Puck>();

            Selection.activeGameObject = puck;
            EditorUtility.DisplayDialog("Success",
                "Puck created!\n\n" +
                "Note: PredictedRigidbody is not added automatically.\n" +
                "Please add it manually from PurrNet components.", "OK");
        }
    }
}
#endif
