using System;
using UnityEngine;
using PurrNet;
using PurrNet.Prediction;


namespace Spinner
{
    /// <summary>
    /// ホッケーのパック
    /// スピナープレイヤーの回転によって打ち出される
    /// </summary>
    public class Puck : PredictedIdentity<Puck.PuckState>
    {
        [Header("設定")]
        [SerializeField, Tooltip("最大速度")]
        private float m_MaxSpeed = 30f;

        [SerializeField, Tooltip("一定速度（最小速度）")]
        private float m_ConstantSpeed = 5f;

        [SerializeField, Tooltip("摩擦（減速率）")]
        private float m_Friction = 0.5f;

        [SerializeField, Tooltip("一定速度を維持するか")]
        private bool m_MaintainConstantSpeed = true;

        [SerializeField, Tooltip("壁での反射時の速度維持率")]
        private float m_WallBounceRetention = 0.8f;

        [SerializeField, Tooltip("ゴール判定用のレイヤー")]
        private LayerMask m_GoalLayer;

        [Header("ダメージ設定")]
        [SerializeField, Tooltip("ダメージを与える最低速度")]
        private float m_DamageThreshold = 10f;

        [SerializeField, Tooltip("速度あたりのダメージ倍率")]
        private float m_DamageMultiplier = 1.0f;

        [SerializeField, Tooltip("最大ダメージ")]
        private float m_MaxDamage = 1.0f;

        private PredictedRigidbody m_Rigidbody;

        // イベント
        public Action<int> OnGoalScored; // int = ゴールしたチームID

        public struct PuckState : IPredictedData<PuckState>
        {
            public Vector3 Velocity;
            public bool IsActive;

            public void Dispose()
            {
            }
        }

        private void Awake()
        {
            m_Rigidbody = GetComponent<PredictedRigidbody>();
        }

        protected override void LateAwake()
        {
        }

        private void OnEnable()
        {
            if (m_Rigidbody != null)
            {
                m_Rigidbody.onCollisionEnter += OnCollisionStart;
            }
        }

        private void OnDisable()
        {
            if (m_Rigidbody != null)
            {
                m_Rigidbody.onCollisionEnter -= OnCollisionStart;
            }
        }

        private void OnCollisionStart(GameObject other, PhysicsCollision collision)
        {
            // nullチェック
            if (other == null) return;

            // 壁との衝突処理
            if (other.CompareTag(Tags.Wall))
            {
                // 反射処理はRigidbodyの物理演算に任せる
                if (m_Rigidbody != null)
                {
                    if (m_MaintainConstantSpeed)
                    {
                        // 一定速度を維持する場合は、速度の大きさを保持
                        float currentSpeed = m_Rigidbody.velocity.magnitude;
                        Vector3 newDirection = m_Rigidbody.velocity.normalized;

                        // 速度の大きさを維持（最大速度は超えない）
                        float maintainedSpeed = Mathf.Min(currentSpeed, m_MaxSpeed);
                        // 最低でも一定速度は保つ
                        maintainedSpeed = Mathf.Max(maintainedSpeed, m_ConstantSpeed);

                        m_Rigidbody.velocity = newDirection * maintainedSpeed;
                    }
                    else
                    {
                        // 一定速度維持モードでない場合は従来通り減衰
                        m_Rigidbody.velocity *= m_WallBounceRetention;
                    }
                }
            }

            // IDamageableを実装しているオブジェクトとの衝突処理
            var damageable = other.GetComponent<IDamageable>();
            if (damageable != null && damageable.IsAlive)
            {
                float currentSpeed = GetCurrentSpeed();

                // 速度が閾値を超えている場合のみダメージを与える
                //if (currentSpeed >= m_DamageThreshold)
                {
                    // 速度に応じたダメージを計算
                    float damage = (currentSpeed - m_DamageThreshold) * m_DamageMultiplier;
                    damage = Mathf.Min(damage, m_MaxDamage);

                    damageable.TakeDamage(damage, this);

                    AppLogger.Log($"パックがダメージ与えた: {damage} damage to {other.name} at speed {currentSpeed}");
                }
            }
        }

        /// <summary>
        /// スピナーからの衝撃を適用
        /// </summary>
        public void ApplyImpact(Vector3 force)
        {
            if (m_Rigidbody == null) return;

            // 既存の速度に衝撃を加算
            Vector3 newVelocity = m_Rigidbody.velocity + force;

            // 最大速度を制限
            if (newVelocity.magnitude > m_MaxSpeed)
            {
                newVelocity = newVelocity.normalized * m_MaxSpeed;
            }

            // 一定速度を維持する場合は、最低速度も保証
            if (m_MaintainConstantSpeed && newVelocity.magnitude < m_ConstantSpeed)
            {
                newVelocity = newVelocity.normalized * m_ConstantSpeed;
            }

            m_Rigidbody.velocity = newVelocity;
        }

        /// <summary>
        /// 一定速度の設定を変更
        /// </summary>
        public void SetConstantSpeed(float speed, bool maintainConstant = true)
        {
            m_ConstantSpeed = speed;
            m_MaintainConstantSpeed = maintainConstant;
        }

        /// <summary>
        /// 現在の速度を取得
        /// </summary>
        public float GetCurrentSpeed()
        {
            return m_Rigidbody != null ? m_Rigidbody.velocity.magnitude : 0f;
        }

        /// <summary>
        /// パックをリセット（ゴール後など）
        /// </summary>
        public void ResetPuck(Vector3 position, Vector3 initialVelocity = default)
        {
            transform.position = position;
            if (m_Rigidbody != null)
            {
                // 初期速度が指定されていない場合は、ランダムな方向に一定速度で開始
                if (initialVelocity == Vector3.zero && m_MaintainConstantSpeed)
                {
                    Vector3 randomDirection = new Vector3(
                        UnityEngine.Random.Range(-1f, 1f),
                        0f,
                        UnityEngine.Random.Range(-1f, 1f)
                    ).normalized;
                    initialVelocity = randomDirection * m_ConstantSpeed;
                }

                m_Rigidbody.velocity = initialVelocity;
                m_Rigidbody.angularVelocity = Vector3.zero;
            }
        }

        protected override void Simulate(ref PuckState state, float delta)
        {
            if (m_Rigidbody != null)
            {
                Vector3 velocity = m_Rigidbody.velocity;

                if (m_MaintainConstantSpeed)
                {
                    // 一定速度を維持するモード
                    if (velocity.magnitude > 0.1f) // わずかでも動いている場合
                    {
                        // 現在の方向を保持しつつ、速度を一定に保つ
                        Vector3 direction = velocity.normalized;
                        float currentSpeed = velocity.magnitude;

                        // 最大速度を超えている場合は制限
                        if (currentSpeed > m_MaxSpeed)
                        {
                            velocity = direction * m_MaxSpeed;
                        }
                        // 一定速度を下回っている場合は、一定速度まで上げる
                        else if (currentSpeed < m_ConstantSpeed)
                        {
                            velocity = direction * m_ConstantSpeed;
                        }
                        // それ以外は現在の速度を維持（摩擦を適用しない）
                    }
                    else
                    {
                        // 完全に停止している場合は、ランダムな方向に動き始める
                        Vector3 randomDirection = new Vector3(
                            UnityEngine.Random.Range(-1f, 1f),
                            0f,
                            UnityEngine.Random.Range(-1f, 1f)
                        ).normalized;
                        velocity = randomDirection * m_ConstantSpeed;
                    }
                }
                else
                {
                    // 従来の摩擦による減速
                    velocity = Vector3.MoveTowards(velocity, Vector3.zero, m_Friction * delta);
                }

                m_Rigidbody.velocity = velocity;
                state.Velocity = velocity;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.2f);

            if (m_Rigidbody != null)
            {
                // 現在の速度ベクトル
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, transform.position + m_Rigidbody.velocity * 0.5f);

                // 一定速度を維持する場合は、最小速度の円も表示
                if (m_MaintainConstantSpeed)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireSphere(transform.position, m_ConstantSpeed * 0.1f);
                }
            }
        }
    }
}
