using System;
using UnityEngine;

namespace JPS
{
    public class FollowTargetUI : MonoBehaviour
    {
        public bool IsEnabled { get; set; } = true;
        public Transform m_Target; // 追従先
        private Quaternion initialLocalRotation;

        private void Awake()
        {
            initialLocalRotation = transform.localRotation;
            transform.SetParent(null);
        }

        private void LateUpdate()
        {
            if (!IsEnabled || m_Target == null) return;

            transform.position = m_Target.position;
            transform.localRotation = initialLocalRotation;
        }
    }
}
