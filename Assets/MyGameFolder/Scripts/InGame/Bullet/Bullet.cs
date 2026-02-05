using System;
using JPS;
using JPS.System;
using PurrNet;
using PurrNet.Prediction;
using UnityEngine;

public class Bullet : PredictedIdentity<Bullet.State>
{
    [SerializeField]
    private int m_Damage = 10;
    private PredictedRigidbody m_Rigidbody;


    private void Awake()
    {
        m_Rigidbody = GetComponent<PredictedRigidbody>();
        if (m_Rigidbody == null)
        {
            Debug.LogError($"Bullet: PredictedRigidbody component missing on {gameObject.name}");
        }
    }

    private void OnEnable()
    {
        if (m_Rigidbody != null)
        {
            m_Rigidbody.onTriggerEnter += OnTriggerImpl;
        }
    }

    private void OnDisable()
    {
        if (m_Rigidbody != null)
        {
            m_Rigidbody.onTriggerEnter -= OnTriggerImpl;
        }
    }

    private void OnTriggerImpl(GameObject other)
    {
        bool shouldDelete = true;
        // if (other.TryGetComponent(out PlayerHealth playerHealth))
        // {
        //     if (playerHealth.owner != this.owner)
        //     {
        //         playerHealth.TakeDamage(m_Damage);
        //         AppLogger.Log($"myOwner{owner} otherOwner{playerHealth.owner}");
        //     }
        //     else
        //     {
        //         shouldDelete = false;
        //     }
        // }

        if (shouldDelete)
        {
            if (predictionManager != null && predictionManager.hierarchy != null)
            {
                predictionManager.hierarchy.Delete(gameObject);
            }
            else
            {
                Debug.LogError($"Bullet: predictionManager or hierarchy is null on {gameObject.name}");
            }
        }
    }

    public struct State : IPredictedData<State>
    {
        public void Dispose()
        {
        }
    }
}