using System;
using PurrNet;
using PurrNet.Prediction;
using UnityEngine;

namespace Spinner
{
    public class PlayerHealth : PredictedIdentity<PlayerHealth.HealthState>, IDamageable
    {

        [SerializeField]
        private int m_MaxHealth = 3;

        [SerializeField, Tooltip("このプレイヤーのチームID（0または1）")]
        private int m_TeamId;

        [SerializeField]
        private ParticleSystem m_DeathEffect;

        [Header("Debug")]
        [SerializeField]
        private int m_DebugDamage = 1;

        private PredictedEvent m_OnDeath;

        // イベント
        public Action<float> OnDamageReceived; // float = ダメージ量
        public Action OnDeathEvent; // 死亡時

        public static event Action<PlayerID?> OnDeathHandler;
        public static Action ClearPlayerHandler;

        // IDamageableの実装
        public float CurrentHealth => currentState.Health;
        public float MaxHealth => m_MaxHealth;
        public bool IsAlive => !currentState.IsDead && currentState.Health > 0;
        public int TeamId => m_TeamId;

        private PlayerUIManager PlayerUIManager => ServiceLocator.Service<PlayerUIManager>();

        protected override void LateAwake()
        {
            m_OnDeath = new PredictedEvent(predictionManager, this);
            m_OnDeath.AddListener(OnDeath);
            ClearPlayerHandler += OnClearPlayers;

            if (owner.HasValue)
            {
                // UIを登録
                if (PlayerUIManager != null)
                {
                    // プレイヤーインデックスを取得
                    int playerIndex = predictionManager.players.currentState.players.IndexOf(owner.Value);
                    if (playerIndex >= 0)
                    {
                        PlayerUIManager.CreateAndRegisterPlayerUI(owner.Value, playerIndex);
                    }
                }
            }
        }

        protected override void OnDestroy()
        {
            m_OnDeath.RemoveListener(OnDeath);
            ClearPlayerHandler -= OnClearPlayers;
        }

        private void OnDeath()
        {
            if (m_DeathEffect != null)
            {
                Instantiate(m_DeathEffect, transform.position, Quaternion.identity);
            }

            PlayerUIManager?.UnregisterPlayerUI(owner.Value);
        }

        private void OnClearPlayers()
        {
            // UIを解除（ゲーム終了時のみ）
            if (owner.HasValue)
            {
                var uiManager = ServiceLocator.Service<PlayerUIManager>();
                uiManager?.UnregisterPlayerUI(owner.Value);
            }

            DestroyPlayer();
        }

        private void DestroyPlayer()
        {
            predictionManager.hierarchy.Delete(gameObject);
        }

        protected override HealthState GetInitialState()
        {
            // ownerが設定されている場合は名前を設定
            string playerName = "Player";
            if (owner.HasValue)
            {
                playerName = $"Player_{owner.Value}";
            }

            HealthState initialState = new HealthState
            {
                Health = m_MaxHealth,
                PlayerName = playerName
            };
            return initialState;
        }

        public void ChangeDamage(int damage)
        {
            currentState.Health += damage;
            currentState.Health = Mathf.Clamp(currentState.Health, 0, m_MaxHealth);
        }

        /// <summary>
        /// ダメージを受ける（IDamageableの実装）
        /// </summary>
        public void TakeDamage(float damage, object source = null)
        {
            if (damage <= 0) return;

            int damageInt = Mathf.CeilToInt(damage);
            ChangeDamage(-damageInt);

            OnDamageReceived?.Invoke(damage);

            string sourceName = source != null ? source.GetType().Name : "Unknown";

            if (currentState.Health <= 0 && !currentState.IsDead)
            {
                currentState.IsDead = true;
                m_OnDeath?.Invoke();
                OnDeathEvent?.Invoke();
                OnDeathHandler?.Invoke(owner);

                DestroyPlayer();
            }
        }

        /// <summary>
        /// ダメージを受ける（整数版、既存コードとの互換性のため）
        /// </summary>
        public void TakeDamage(int damage)
        {
            TakeDamage((float)damage, null);
        }

        /// <summary>
        /// 体力を回復
        /// </summary>
        public void Heal(float amount)
        {
            if (amount <= 0) return;

            int healInt = Mathf.CeilToInt(amount);
            ChangeDamage(healInt);
        }

        /// <summary>
        /// 体力をリセット
        /// </summary>
        public void ResetHealth()
        {
            currentState.Health = m_MaxHealth;
            currentState.IsDead = false;
        }

        #region Debug Commands

        /// <summary>
        /// デバッグ用：ダメージを与える
        /// </summary>
        [ContextMenu("Debug/ダメージを与える")]
        private void DebugTakeDamage()
        {
            TakeDamage(m_DebugDamage);
            Debug.Log($"[Debug] {m_DebugDamage} ダメージを与えました。現在のHP: {currentState.Health}/{m_MaxHealth}");
        }

        /// <summary>
        /// デバッグ用：即死
        /// </summary>
        [ContextMenu("Debug/即死")]
        private void DebugInstantKill()
        {
            TakeDamage(m_MaxHealth);
            Debug.Log($"[Debug] 即死ダメージを与えました。");
        }

        #endregion

        /// <summary>
        /// プレイヤー名を設定（通常は自動設定されるが、動的に変更する場合に使用）
        /// </summary>
        public void SetPlayerName(string playerName)
        {
            if (string.IsNullOrEmpty(playerName))
            {
                playerName = "Player";
            }

            currentState.PlayerName = playerName;

            // 即座にUIを更新
            if (owner.HasValue)
            {
                var uiManager = ServiceLocator.Service<PlayerUIManager>();
                uiManager?.UpdatePlayerName(owner.Value, playerName);
            }
        }

        protected override void UpdateView(HealthState viewState, HealthState? verified)
        {
            base.UpdateView(viewState, verified);

            // PlayerUIManager経由で体力とプレイヤー名を更新
            if (owner.HasValue)
            {
                if (PlayerUIManager != null)
                {
                    PlayerUIManager.UpdatePlayerHealth(owner.Value, viewState.Health, m_MaxHealth);

                    // プレイヤー名が設定されている場合のみ更新
                    if (!string.IsNullOrEmpty(viewState.PlayerName))
                    {
                        PlayerUIManager.UpdatePlayerName(owner.Value, viewState.PlayerName);
                    }
                }
            }
        }


        public struct HealthState : IPredictedData<HealthState>
        {
            public int Health;
            public bool IsDead;
            public string PlayerName;

            public void Dispose()
            {
            }

            public override string ToString()
            {
                return $"{Health}";
            }
        }
    }
}