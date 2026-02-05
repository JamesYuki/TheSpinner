using UnityEngine;
using UnityEngine.InputSystem;

namespace JPS
{
    using System;
    public class PlayerInputHandler : MonoBehaviour
    {
        private InputSystem_Actions m_InputActions;
        private Camera m_MainCamera;

        [SerializeField, Header("マウス操作を有効にするか")]
        private bool m_EnableMouse = true;

        private void Awake()
        {
            m_InputActions = new InputSystem_Actions();
        }

        private void OnEnable()
        {
            m_InputActions.Enable();
            m_MainCamera = ServiceLocator.Service<CameraManager>().MainCamera;
        }

        private void OnDisable()
        {
            m_InputActions.Disable();
            m_MainCamera = null;
        }

        public Vector2 GetMove()
        {
            return m_InputActions.Player.Move.ReadValue<Vector2>();
        }

        public bool GetJump()
        {
            return m_InputActions.Player.Jump.triggered;
        }

        // 右スティックの傾き（Look入力）を返す
        public Vector2 GetLookDirection()
        {
            // マウス操作かどうか判定（Mouse.currentが有効かつ左ボタン押下中）
            if (m_EnableMouse)
            {
                // プレイヤーの位置（Yは地面）
                var playerPos = transform.position;
                var mouseWorld = GetMouseWorldPosition(playerPos.y);
                var dir = mouseWorld - playerPos;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.01f)
                {
                    // XZ平面の方向をVector2で返す
                    return new Vector2(dir.x, dir.z).normalized;
                }
                return Vector2.zero;
            }
            // コントローラー等は従来通り
            return m_InputActions.Player.Look.ReadValue<Vector2>();
        }

        // マウスのワールド座標を返す（地面Y=transform.position.yで取得）
        public Vector3 GetMouseWorldPosition(float planeY = 0f)
        {
            var mousePos = Mouse.current.position.ReadValue();
            Ray ray = m_MainCamera.ScreenPointToRay(mousePos);
            Plane plane = new Plane(Vector3.up, new Vector3(0, planeY, 0));
            if (plane.Raycast(ray, out float enter))
            {
                return ray.GetPoint(enter);
            }
            return Vector3.zero;
        }

        // 攻撃ボタンを押した瞬間
        public bool GetAttackDown()
        {
            return m_InputActions.Player.Attack.WasPressedThisFrame();
        }

        // 攻撃ボタンを押し続けている間
        public bool GetAttack()
        {
            return m_InputActions.Player.Attack.IsPressed();
        }
    }
}
