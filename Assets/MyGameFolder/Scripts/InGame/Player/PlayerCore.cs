using System;
using UnityEngine;
using PurrNet;
using PurrNet.Prediction;

namespace Spinner
{
    public class PlayerCore : PredictedIdentity<PlayerCore.State>, IDamageable
    {

        public struct State : IPredictedData<State>
        {
            public int TeamId;
            public float Health;

            public void Dispose()
            {
            }
        }

        [Header("プレイヤー設定")]
        [SerializeField, Tooltip("このプレイヤーのチームID（0または1）")]
        private int m_TeamId;

        [SerializeField, Tooltip("最大体力")]
        private float m_MaxHealth = 3.0f;

        private PredictedRigidbody m_Rigidbody;
        private float m_CurrentHealth;

        // イベント
        public Action<float> OnDamageReceived; // float = ダメージ量
        public Action OnDeath; // 死亡時

        public float CurrentHealth => m_CurrentHealth;
        public float MaxHealth => m_MaxHealth;
        public bool IsAlive => m_CurrentHealth > 0;
        public int TeamId => m_TeamId;

        private void Awake()
        {
            m_Rigidbody = GetComponent<PredictedRigidbody>();
            m_CurrentHealth = m_MaxHealth;
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
        }

        /// <summary>
        /// ダメージを受ける
        /// </summary>
        public void TakeDamage(float damage, object source = null)
        {
            if (damage <= 0) return;

            m_CurrentHealth -= damage;
            m_CurrentHealth = Mathf.Max(m_CurrentHealth, 0);

            OnDamageReceived?.Invoke(damage);

            // 体力が0になったら死亡処理
            if (m_CurrentHealth <= 0)
            {
                OnDeath?.Invoke();
            }

            string sourceName = source != null ? source.GetType().Name : "Unknown";
            Debug.Log($"Player {m_TeamId} took {damage:F1} damage from {sourceName}. Health: {m_CurrentHealth:F1}/{m_MaxHealth}");
        }

        /// <summary>
        /// 体力を回復
        /// </summary>
        public void Heal(float amount)
        {
            if (amount <= 0) return;

            m_CurrentHealth += amount;
            m_CurrentHealth = Mathf.Min(m_CurrentHealth, m_MaxHealth);
        }

        /// <summary>
        /// 体力をリセット
        /// </summary>
        public void ResetHealth()
        {
            m_CurrentHealth = m_MaxHealth;
        }

        protected override void Simulate(ref State state, float delta)
        {
            state.TeamId = m_TeamId;
            state.Health = m_CurrentHealth;
        }
    }
}
