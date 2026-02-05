using UnityEngine;

namespace Spinner
{
    /// <summary>
    /// スピナープレイヤーの設定データ
    /// ScriptableObjectで管理し、調整を容易にする
    /// </summary>
    [CreateAssetMenu(fileName = "SpinnerSettings", menuName = "Spinner/SpinnerSettings")]
    public class SpinnerSettings : ScriptableObject
    {
        [Header("回転パラメータ")]
        [SerializeField, Tooltip("最大角速度（度/秒）")]
        private float m_MaxAngularVelocity = 720f;

        [SerializeField, Tooltip("角加速度（度/秒²）")]
        private float m_AngularAcceleration = 1000f;

        [SerializeField, Tooltip("自然減速（入力なし時）")]
        private float m_NaturalDeceleration = 200f;

        [SerializeField, Tooltip("ブレーキ時の減速")]
        private float m_BrakeDeceleration = 2000f;

        [Header("回転制限")]
        [SerializeField, Tooltip("回転制限を有効にする")]
        private bool m_UseRotationLimit = true;

        [SerializeField, Tooltip("左回転の最大角度（正の値、度）")]
        private float m_MaxLeftRotation = 180f;

        [SerializeField, Tooltip("右回転の最大角度（正の値、度）")]
        private float m_MaxRightRotation = 180f;

        [Header("アーム設定")]
        [SerializeField, Tooltip("アームの長さ（コアからの距離）")]
        private float m_ArmLength = 2f;

        [SerializeField, Tooltip("アームの当たり判定の幅")]
        private float m_ArmWidth = 0.5f;

        [Header("パック衝突")]
        [SerializeField, Tooltip("パックへの衝撃力係数")]
        private float m_PuckImpactMultiplier = 1.5f;

        [SerializeField, Tooltip("最大衝撃速度")]
        private float m_MaxImpactVelocity = 50f;

        [Header("リスポーン")]
        [SerializeField, Tooltip("死亡後のリスポーン時間（秒）")]
        private float m_RespawnTime = 3f;

        // プロパティ
        public float MaxAngularVelocity => m_MaxAngularVelocity;
        public float AngularAcceleration => m_AngularAcceleration;
        public float NaturalDeceleration => m_NaturalDeceleration;
        public float BrakeDeceleration => m_BrakeDeceleration;
        public bool UseRotationLimit => m_UseRotationLimit;
        public float MaxLeftRotation => m_MaxLeftRotation;
        public float MaxRightRotation => m_MaxRightRotation;
        public float ArmLength => m_ArmLength;
        public float ArmWidth => m_ArmWidth;
        public float PuckImpactMultiplier => m_PuckImpactMultiplier;
        public float MaxImpactVelocity => m_MaxImpactVelocity;
        public float RespawnTime => m_RespawnTime;
    }
}
