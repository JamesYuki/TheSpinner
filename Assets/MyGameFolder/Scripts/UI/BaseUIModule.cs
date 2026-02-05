using UnityEngine;

namespace JPS.UI
{
    [RequireComponent(typeof(Canvas))]
    public abstract class BaseUIModule : MonoBehaviour
    {

        protected virtual void RegisterServiceLocator() { }
        protected virtual void UnregisterServiceLocator() { }

        protected virtual void Awake()
        {
            RegisterServiceLocator();
        }

        protected virtual void OnDestroy()
        {
            UnregisterServiceLocator();
        }
    }
}

