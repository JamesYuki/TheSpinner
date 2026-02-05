using System;
using JPS;
using PurrNet;
using PurrNet.Prediction;
using TMPro;
using UnityEngine;

public class PlayerHealth : PredictedIdentity<PlayerHealth.HealthState>
{

    [SerializeField]
    private int m_MaxHealth = 100;

    [SerializeField]
    private TextMeshProUGUI m_HealthText;

    [SerializeField]
    private ParticleSystem m_DeathEffect;

    private PredictedEvent m_OnDeath;

    public static event Action<PlayerID?> OnDeathHandler;
    public static Action ClearPlayerHandler;

    protected override void LateAwake()
    {
        m_OnDeath = new PredictedEvent(predictionManager, this);
        m_OnDeath.AddListener(OnDeath);
        ClearPlayerHandler += OnClearPlayers;
    }

    protected override void OnDestroy()
    {
        m_OnDeath.RemoveListener(OnDeath);
        ClearPlayerHandler -= OnClearPlayers;
    }

    private void OnDeath()
    {
        Destroy(m_HealthText.gameObject);
        if (m_DeathEffect == null)
        {
            return;
        }

        Instantiate(m_DeathEffect, transform.position, Quaternion.identity);
    }

    private void OnClearPlayers()
    {
        DestroyPlayer();
    }

    private void DestroyPlayer()
    {
        predictionManager.hierarchy.Delete(gameObject);
    }

    protected override HealthState GetInitialState()
    {
        HealthState initialState = new HealthState
        {
            Health = m_MaxHealth
        };
        return initialState;
    }

    public void ChangeDamage(int damage)
    {
        currentState.Health += damage;
        currentState.Health = Mathf.Clamp(currentState.Health, 0, m_MaxHealth);
    }

    public void TakeDamage(int damage)
    {
        ChangeDamage(-damage);

        if (currentState.Health <= 0 && !currentState.IsDead)
        {
            currentState.IsDead = true;
            m_OnDeath?.Invoke();
            OnDeathHandler?.Invoke(owner);

            DestroyPlayer();
        }
    }

    protected override void UpdateView(HealthState viewState, HealthState? verified)
    {
        base.UpdateView(viewState, verified);

        if (m_HealthText == null)
        {
            return;
        }

        m_HealthText.text = $"$ {viewState.Health}";
    }


    public struct HealthState : IPredictedData<HealthState>
    {
        public int Health;
        public bool IsDead;
        public void Dispose()
        {
        }

        public override string ToString()
        {
            return $"Health: {Health}";
        }
    }

}
