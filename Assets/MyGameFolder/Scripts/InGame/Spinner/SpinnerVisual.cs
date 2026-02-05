using UnityEngine;

namespace Spinner
{
    /// <summary>
    /// スピナープレイヤーの見た目を管理
    /// == 〇 == の形状を実現
    /// </summary>
    public class SpinnerVisual : MonoBehaviour
    {
        [Header("コンポーネント参照")]
        [SerializeField]
        private SpinnerController m_Controller;

        [SerializeField, Tooltip("コアのRenderer")]
        private Renderer m_CoreRenderer;

        [SerializeField, Tooltip("左アームのRenderer")]
        private Renderer m_LeftArmRenderer;

        [SerializeField, Tooltip("右アームのRenderer")]
        private Renderer m_RightArmRenderer;

        [Header("見た目設定")]
        [SerializeField, Tooltip("回転速度に応じたエミッションの強度")]
        private float m_EmissionIntensityMultiplier = 0.01f;

        [SerializeField, Tooltip("待機中のエミッションカラー")]
        private Color m_IdleColor = Color.gray;

        [SerializeField, Tooltip("通常時のエミッションカラー")]
        private Color m_ActiveColor = Color.cyan;

        [SerializeField, Tooltip("死亡中のエミッションカラー")]
        private Color m_DeadColor = Color.red;

        [Header("トレイル設定")]
        [SerializeField]
        private TrailRenderer m_LeftArmTrail;

        [SerializeField]
        private TrailRenderer m_RightArmTrail;

        [SerializeField, Tooltip("トレイルを表示する最小角速度")]
        private float m_TrailMinVelocity = 360f;

        private MaterialPropertyBlock m_PropertyBlock;
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private void Awake()
        {
            m_PropertyBlock = new MaterialPropertyBlock();

            if (m_Controller == null)
            {
                m_Controller = GetComponentInParent<SpinnerController>();
            }
        }

        private void OnEnable()
        {
            if (m_Controller != null)
            {
                m_Controller.OnAngularVelocityChanged += OnAngularVelocityChanged;
            }
        }

        private void OnDisable()
        {
            if (m_Controller != null)
            {
                m_Controller.OnAngularVelocityChanged -= OnAngularVelocityChanged;
            }
        }

        private void Update()
        {
            if (m_Controller == null) return;

            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            // 現在のステートに応じてカラーを変更
            // Color targetColor = GetStateColor(m_Controller.CurrentStateType);
            float intensity = Mathf.Abs(m_Controller.CurrentAngularVelocity) * m_EmissionIntensityMultiplier;
            // Color emissionColor = targetColor * (1f + intensity);

            // マテリアルプロパティを更新
            // UpdateRendererEmission(m_CoreRenderer, emissionColor);
            // UpdateRendererEmission(m_LeftArmRenderer, emissionColor);
            // UpdateRendererEmission(m_RightArmRenderer, emissionColor);

            // トレイルの表示/非表示
            bool showTrail = Mathf.Abs(m_Controller.CurrentAngularVelocity) > m_TrailMinVelocity;
            if (m_LeftArmTrail != null) m_LeftArmTrail.emitting = showTrail;
            if (m_RightArmTrail != null) m_RightArmTrail.emitting = showTrail;
        }

        private Color GetStateColor(SpinnerPlayerStateType stateType)
        {
            return stateType switch
            {
                SpinnerPlayerStateType.Idle => m_IdleColor,
                SpinnerPlayerStateType.Active => m_ActiveColor,
                SpinnerPlayerStateType.Dead => m_DeadColor,
                _ => m_ActiveColor
            };
        }

        private void UpdateRendererEmission(Renderer renderer, Color emissionColor)
        {
            if (renderer == null) return;

            renderer.GetPropertyBlock(m_PropertyBlock);
            m_PropertyBlock.SetColor(EmissionColorId, emissionColor);
            renderer.SetPropertyBlock(m_PropertyBlock);
        }

        private void OnAngularVelocityChanged(float angularVelocity)
        {
            // 角速度変化時の追加処理（パーティクルなど）
        }
    }
}
