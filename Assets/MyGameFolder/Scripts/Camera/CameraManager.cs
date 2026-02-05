using UnityEngine;
using Unity.Cinemachine;
using System;

namespace Spinner
{
    public class CameraManager : MonoBehaviour
    {
        [SerializeField]
        private Camera m_MainCamera;

        public Camera MainCamera => m_MainCamera;

        [SerializeField]
        private CinemachineCamera m_CinemachineCamera;

        public CinemachineCamera CinemachineCamera => m_CinemachineCamera;

        [SerializeField]
        private Transform m_FollowTargetPoint;

        private void Awake()
        {
            ServiceLocator.Register(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<CameraManager>();
        }

        public void SetLookAtTarget(Transform target)
        {
            m_FollowTargetPoint.SetParent(target);
            m_FollowTargetPoint.localPosition = Vector3.zero;
            m_CinemachineCamera.Follow = m_FollowTargetPoint;
        }
    }
}
