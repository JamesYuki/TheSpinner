using UnityEngine;

/// <summary>
/// 親の回転（Rotation）のみ無視するGameObjectにアタッチするクラス。
/// 毎フレーム、ワールド回転を維持します。
/// </summary>
public class IgnoreParentRotation : MonoBehaviour
{
    private Quaternion initialRotation;

    void Awake()
    {
        // 初期ワールド回転のみ記録
        initialRotation = transform.rotation;
    }

    void LateUpdate()
    {
        // 親の回転のみ打ち消す
        transform.rotation = initialRotation;
    }
}
