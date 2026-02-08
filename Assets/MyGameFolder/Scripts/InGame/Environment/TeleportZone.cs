using UnityEngine;

namespace Spinner
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    public class TeleportZone : MonoBehaviour
    {
        [Header("テレポート設定")]
        [SerializeField, Tooltip("テレポートの色ID（同じ色がペア）")]
        private TeleportColorId m_ColorId;

        [SerializeField, Tooltip("所属エリア（0 = 1P左側, 1 = 2P右側）")]
        private int m_TeamSide;

        [Header("出口設定")]
        [SerializeField, Tooltip("テレポート出口の位置（パックが出現するポイント）")]
        private Transform m_ExitPoint;

        [SerializeField, Tooltip("出口方向（パックの射出方向）。未設定の場合はtransform.forwardを使用")]
        private Transform m_ExitDirection;

        [Header("ビジュアル")]
        [SerializeField, Tooltip("テレポートゾーンのレンダラー（色反映用）")]
        private Renderer m_Renderer;

        [SerializeField, Tooltip("テレポート時のエフェクト")]
        private ParticleSystem m_TeleportEffect;

        [SerializeField]
        private float m_ExitForceMultiplier = 10.0f;

        /// <summary>パックが進入した時の速度を一時保存</summary>
        private float m_SavedEntrySpeed;

        /// <summary>デバッグ用: 最後のテレポート出口速度</summary>
        private Vector3 m_LastExitVelocity;
        private bool m_HasTeleported;

        /// <summary>テレポートの色ID</summary>
        public TeleportColorId ColorId => m_ColorId;

        /// <summary>
        /// ColorIdを動的に変更する（シャッフル用）
        /// </summary>
        public void SetColorId(TeleportColorId newColorId, Color visualColor)
        {
            m_ColorId = newColorId;
            ApplyVisualColor(visualColor);
        }

        /// <summary>所属エリア (0=1P, 1=2P)</summary>
        public int TeamSide => m_TeamSide;

        /// <summary>
        /// テレポート出口のワールド座標を取得。
        /// m_ExitPoint が設定されていない場合は自身の位置を返す。
        /// </summary>
        public Vector3 ExitPosition =>
            m_ExitPoint != null ? m_ExitPoint.position : transform.position;

        /// <summary>
        /// 出口方向（正規化済み）。m_ExitDirection が設定されている場合、ExitPointからExitDirectionへの方向ベクトル。
        /// 未設定時は transform.forward。
        /// </summary>
        public Vector3 ExitForward
        {
            get
            {
                Vector3 direction = transform.forward;
                if (m_ExitDirection != null && m_ExitPoint != null)
                {
                    // ExitPointからExitDirectionの位置への方向ベクトル（Y座標は無視）
                    Vector3 fromPos = m_ExitPoint.position;
                    Vector3 toPos = m_ExitDirection.position;
                    direction = (toPos - fromPos).normalized;
                }

                return direction * m_ExitForceMultiplier;
            }
        }

        /// <summary>テレポートエフェクトを再生</summary>
        public void PlayEffect()
        {
            if (m_TeleportEffect != null)
            {
                m_TeleportEffect.Play();
            }
        }

        /// <summary>
        /// ビジュアルカラーを設定する（TeleportManager から呼ばれる）
        /// </summary>
        public void ApplyVisualColor(Color color)
        {
            if (m_Renderer == null) return;

            var block = new MaterialPropertyBlock();
            m_Renderer.GetPropertyBlock(block);
            block.SetColor("_Color", color);
            block.SetColor("_BaseColor", color); // URP
            m_Renderer.SetPropertyBlock(block);
        }

        /// <summary>
        /// パックの進入速度を保存
        /// </summary>
        public void SetEntrySpeed(float speed)
        {
            m_SavedEntrySpeed = speed;
        }

        /// <summary>
        /// 保存された進入速度を取得
        /// </summary>
        public float GetEntrySpeed()
        {
            return m_SavedEntrySpeed;
        }

        /// <summary>
        /// テレポート時の出口速度を記録（デバッグ用）
        /// </summary>
        public void RecordTeleport(Vector3 exitVelocity)
        {
            m_LastExitVelocity = exitVelocity;
            m_HasTeleported = true;
        }

        private void OnValidate()
        {
            // トリガーが設定されていなければ自動で設定
            var col = GetComponent<Collider>();
            if (col != null && !col.isTrigger)
            {
                col.isTrigger = true;
            }

            // Rigidbodyが必要（PurrNetの予測システムでトリガーを動作させるため）
            var rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }

            // Kinematicに設定（動かないオブジェクト）
            if (!rb.isKinematic)
            {
                rb.isKinematic = true;
            }

            // 重力を無効化
            rb.useGravity = false;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // ゾーンの色を表示
            Color gizmoColor = GetGizmoColor();
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(transform.position, 0.5f);

            // 出口位置と方向を表示
            Vector3 exitPos = ExitPosition;
            Vector3 exitDir = ExitForward;

            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(exitPos, 0.15f);
            Gizmos.DrawLine(exitPos, exitPos + exitDir * 1.5f);

            // 最後のテレポート速度ベクトルを表示（黄色の矢印）
            if (m_HasTeleported)
            {
                Gizmos.color = Color.yellow;
                Vector3 velocityEnd = exitPos + m_LastExitVelocity;
                Gizmos.DrawLine(exitPos, velocityEnd);
                DrawArrow(exitPos, velocityEnd);

                // 速度の大きさを表示
                UnityEditor.Handles.Label(
                    velocityEnd,
                    $"Speed: {m_LastExitVelocity.magnitude:F2}",
                    new GUIStyle
                    {
                        normal = new GUIStyleState { textColor = Color.yellow },
                        fontSize = 10
                    }
                );
            }

            // ラベル表示
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 0.8f,
                $"{m_ColorId} (Side:{m_TeamSide})",
                new GUIStyle
                {
                    normal = new GUIStyleState { textColor = gizmoColor },
                    fontSize = 12,
                    fontStyle = FontStyle.Bold
                }
            );
        }

        private Color GetGizmoColor()
        {
            return m_ColorId switch
            {
                TeleportColorId.Red => Color.red,
                TeleportColorId.Blue => Color.blue,
                TeleportColorId.Yellow => Color.yellow,
                TeleportColorId.Green => Color.green,
                TeleportColorId.Purple => new Color(0.6f, 0f, 0.8f),
                _ => Color.white,
            };
        }

        private void DrawArrow(Vector3 start, Vector3 end)
        {
            Vector3 direction = (end - start).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
            float arrowSize = 0.3f;

            Gizmos.DrawLine(end, end - direction * arrowSize + right * arrowSize * 0.5f);
            Gizmos.DrawLine(end, end - direction * arrowSize - right * arrowSize * 0.5f);
        }
#endif
    }
}
