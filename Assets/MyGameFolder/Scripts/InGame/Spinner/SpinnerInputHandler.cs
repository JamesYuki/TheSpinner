using UnityEngine;
using UnityEngine.InputSystem;

namespace Spinner
{
    /// <summary>
    /// スピナープレイヤー専用の入力ハンドラー
    /// A/Dキーまたは左右矢印キーで回転を制御
    /// </summary>
    public class SpinnerInputHandler : MonoBehaviour
    {
        private InputSystem_Actions m_InputActions;

        private void Awake()
        {
            m_InputActions = new InputSystem_Actions();
        }

        private void OnEnable()
        {
            m_InputActions.Enable();
        }

        private void OnDisable()
        {
            m_InputActions.Disable();
        }

        /// <summary>
        /// 回転入力を取得 (-1 ~ 1)
        /// Move入力の左右成分（A/D, 左右矢印, スティック左右）を使用
        /// </summary>
        public float GetRotationInput()
        {
            Vector2 moveInput = m_InputActions.Player.Move.ReadValue<Vector2>();
            return Mathf.Clamp(moveInput.x, -1f, 1f);
        }

        /// <summary>
        /// ブレーキ入力を取得（回転を急停止）
        /// Spaceキーまたはゲームパッドの下ボタン
        /// </summary>
        public bool GetBrakeInput()
        {
            return m_InputActions.Player.Crouch.IsPressed();
        }
    }
}
