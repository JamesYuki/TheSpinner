using UnityEngine;

namespace Spinner
{
    public class EffectAutoDestroyer : MonoBehaviour
    {
        private ParticleSystem effectParticleSystem;

        void OnEnable()
        {
            if (effectParticleSystem == null)
            {
                effectParticleSystem = GetComponent<ParticleSystem>();
            }
            if (effectParticleSystem != null)
            {
                var main = effectParticleSystem.main;
                main.stopAction = ParticleSystemStopAction.Callback;
            }
        }

        private void OnParticleSystemStopped()
        {
            Destroy(gameObject);
        }
    }
}
