using System;
using PurrNet.Prediction.StateMachine;
using UnityEngine;

namespace Spinner
{
    public class InGameUIManager : MonoBehaviour
    {
        [SerializeField, ReadOnly] private IStateUI[] m_InGameUIs;

        private UseRefCounter m_ActiveCounter = new();

        private void Awake()
        {
            ServiceLocator.Register(this);
            m_InGameUIs = GetComponentsInChildren<IStateUI>(true);

            m_ActiveCounter.OnReleased += HideAllUIs;
        }

        private void OnDestroy()
        {
            m_ActiveCounter.OnReleased -= HideAllUIs;

            ServiceLocator.Unregister<InGameUIManager>();
        }

        public IDisposable ActiveStateUI(IPredictedStateNodeBase predictedStateNode, object param = null)
        {
            foreach (var ui in m_InGameUIs)
            {
                ui.Activate(predictedStateNode, param);
            }

            return m_ActiveCounter.Use();
        }

        private void HideAllUIs()
        {
            foreach (var ui in m_InGameUIs)
            {
                ui.Activate(null);
            }
        }

        public void UpdateStateUI(IPredictedStateNodeBase predictedStateNode, object param = null)
        {
            foreach (var ui in m_InGameUIs)
            {
                ui.UpdateUI(predictedStateNode, param);
            }
        }
    }
}
