using System.Collections.Generic;
using PurrNet;
using UnityEngine;
using System;

namespace Spinner
{
    /// <summary>
    /// チームごとのUI設定
    /// </summary>
    [Serializable]
    public class TeamUIData
    {
        [SerializeField, Tooltip("チームID")]
        public int TeamId;

        [SerializeField, Tooltip("このチームのプレイヤーUI一覧")]
        public List<PlayerUIData> PlayerUIs = new List<PlayerUIData>();
    }

    /// <summary>
    /// プレイヤーIDとUIの対応を管理するマネージャー
    /// </summary>
    public class PlayerUIManager : MonoBehaviour
    {
        [SerializeField, Tooltip("各チームのUI設定")]
        private List<TeamUIData> m_TeamUIs = new List<TeamUIData>();

        [SerializeField, Tooltip("PlayerUIのプレハブ")]
        private GameObject m_PlayerUIPrefab;

        [SerializeField, Tooltip("PlayerUIを配置する親Transform")]
        private Transform m_UIParent;

        [SerializeField, Tooltip("チーム割り当て戦略（将来的に動的に変更可能）")]
        private ITeamAssignmentStrategy m_TeamAssignmentStrategy;

        // PlayerIDとUIの対応マップ
        private Dictionary<PlayerID, PlayerUIData> m_PlayerUIMap = new Dictionary<PlayerID, PlayerUIData>();

        // 動的に生成されたUIオブジェクトのリスト
        private List<GameObject> m_DynamicUIObjects = new List<GameObject>();

        private void Awake()
        {
            Debug.Log($"[PlayerUIManager] Awake called on instance {GetInstanceID()}");
            ServiceLocator.Register(this);

            // デフォルトのチーム割り当て戦略を設定
            if (m_TeamAssignmentStrategy == null)
            {
                m_TeamAssignmentStrategy = new AlternateTeamAssignmentStrategy();
            }
        }

        private void OnDestroy()
        {
            Debug.Log($"[PlayerUIManager] OnDestroy called on instance {GetInstanceID()}, MapCount={m_PlayerUIMap.Count}");
            ServiceLocator.Unregister<PlayerUIManager>();
            ClearDynamicUIs();
        }

        /// <summary>
        /// チーム割り当て戦略を設定（将来的に試合前に変更可能）
        /// </summary>
        public void SetTeamAssignmentStrategy(ITeamAssignmentStrategy strategy)
        {
            m_TeamAssignmentStrategy = strategy;
        }

        /// <summary>
        /// プレイヤーIDに対応するUIを登録（既存のUI使用）
        /// </summary>
        /// <param name="playerId">プレイヤーID</param>
        /// <param name="teamId">チームID</param>
        /// <param name="playerIndex">チーム内でのプレイヤーインデックス</param>
        public void RegisterPlayerUI(PlayerID playerId, int teamId, int playerIndex)
        {
            PlayerUIData uiData = GetUIDataForTeam(teamId, playerIndex);
            if (uiData != null)
            {
                m_PlayerUIMap[playerId] = uiData;
                uiData.SetActive(true);
                Debug.Log($"PlayerUI registered: PlayerID={playerId}, TeamID={teamId}, Index={playerIndex}");
            }
            else
            {
                Debug.LogWarning($"UIデータが見つかりません: TeamID={teamId}, Index={playerIndex}");
            }
        }

        /// <summary>
        /// プレイヤーIDに対応するUIを動的に生成して登録
        /// </summary>
        /// <param name="playerId">プレイヤーID</param>
        /// <param name="globalPlayerIndex">全体でのプレイヤーインデックス</param>
        /// <returns>UIが生成されたかどうか</returns>
        public bool CreateAndRegisterPlayerUI(PlayerID playerId, int globalPlayerIndex)
        {
            // 既に登録されている場合はスキップ
            if (m_PlayerUIMap.ContainsKey(playerId))
            {
                Debug.Log($"[PlayerUIManager] PlayerID={playerId} is already registered, Instance={GetInstanceID()}, MapCount={m_PlayerUIMap.Count}, skipping");
                return false;
            }

            if (m_PlayerUIPrefab == null)
            {
                Debug.LogError("PlayerUIプレハブが設定されていません");
                return false;
            }

            // チーム割り当て戦略からチームとインデックスを取得
            var (teamId, teamPlayerIndex) = m_TeamAssignmentStrategy.GetTeamAssignment(globalPlayerIndex, playerId);

            // UIParentのチェック
            if (m_UIParent == null)
            {
                Debug.LogError("UIParentが設定されていません");
                return false;
            }

            // UIを生成
            GameObject uiObject = Instantiate(m_PlayerUIPrefab, m_UIParent);
            m_DynamicUIObjects.Add(uiObject);

            // PlayerUIが独自のCanvasGroupを持つことで、親のCanvasGroupの影響を受けないようにする
            CanvasGroup playerUICanvasGroup = uiObject.GetComponent<CanvasGroup>();
            if (playerUICanvasGroup == null)
            {
                playerUICanvasGroup = uiObject.AddComponent<CanvasGroup>();
            }
            // 常に表示状態を維持
            playerUICanvasGroup.alpha = 1.0f;
            playerUICanvasGroup.interactable = true;
            playerUICanvasGroup.blocksRaycasts = true;

            // PlayerUIDataコンポーネントを取得または追加
            PlayerUIData uiData = uiObject.GetComponent<PlayerUIData>();
            if (uiData == null)
            {
                Debug.LogError("PlayerUIプレハブにPlayerUIDataコンポーネントがありません");
                Destroy(uiObject);
                return false;
            }

            // PlayerIDとUIを紐づけ
            m_PlayerUIMap[playerId] = uiData;
            uiData.SetActive(true);

            // ヒエラルキー構造をログ出力
            string hierarchyPath = GetHierarchyPath(uiObject.transform);
            Debug.Log($"[PlayerUIManager] PlayerUI created and registered: PlayerID={playerId}, TeamID={teamId}, TeamIndex={teamPlayerIndex}, Instance={GetInstanceID()}, TotalInMap={m_PlayerUIMap.Count}, HierarchyPath={hierarchyPath}");
            return true;
        }

        /// <summary>
        /// プレイヤーIDに対応するUIの登録を解除
        /// </summary>
        public void UnregisterPlayerUI(PlayerID playerId)
        {
            if (m_PlayerUIMap.TryGetValue(playerId, out var uiData))
            {
                uiData.SetActive(false);
                m_PlayerUIMap.Remove(playerId);
            }
        }
        /// <summary>
        /// プレイヤーIDに対応するUIデータを取得
        /// </summary>
        public PlayerUIData GetPlayerUI(PlayerID playerId)
        {
            m_PlayerUIMap.TryGetValue(playerId, out var uiData);
            return uiData;
        }

        /// <summary>
        /// チームとインデックスからUIデータを取得
        /// </summary>
        private PlayerUIData GetUIDataForTeam(int teamId, int playerIndex)
        {
            // 指定されたチームIDのTeamUIDataを検索
            TeamUIData teamUI = m_TeamUIs.Find(t => t.TeamId == teamId);

            if (teamUI != null && playerIndex >= 0 && playerIndex < teamUI.PlayerUIs.Count)
            {
                return teamUI.PlayerUIs[playerIndex];
            }

            return null;
        }

        /// <summary>
        /// すべてのUIをクリア
        /// </summary>
        public void ClearAllUIs()
        {
            Debug.Log($"[PlayerUIManager] ClearAllUIs called, Instance={GetInstanceID()}, MapCount={m_PlayerUIMap.Count}");
            foreach (var uiData in m_PlayerUIMap.Values)
            {
                uiData.SetActive(false);
            }
            m_PlayerUIMap.Clear();
            ClearDynamicUIs();
        }

        /// <summary>
        /// 動的に生成されたUIをすべて破棄
        /// </summary>
        private void ClearDynamicUIs()
        {
            foreach (var uiObj in m_DynamicUIObjects)
            {
                if (uiObj != null)
                {
                    Destroy(uiObj);
                }
            }
            m_DynamicUIObjects.Clear();
        }

        /// <summary>
        /// 指定したPlayerIDの体力を更新
        /// </summary>
        public void UpdatePlayerHealth(PlayerID playerId, int health, int maxHealth)
        {
            if (m_PlayerUIMap.TryGetValue(playerId, out var uiData))
            {
                // UIオブジェクトがnullでないか確認
                if (uiData == null || uiData.gameObject == null)
                {
                    m_PlayerUIMap.Remove(playerId);
                    Debug.LogWarning($"[PlayerUIManager] Removed null entry from map, NewMapCount={m_PlayerUIMap.Count}");
                    return;
                }
                uiData.UpdateHealthText(health, maxHealth);
            }
        }

        /// <summary>
        /// 指定したPlayerIDのプレイヤー名を更新
        /// </summary>
        public void UpdatePlayerName(PlayerID playerId, string playerName)
        {
            if (m_PlayerUIMap.TryGetValue(playerId, out var uiData))
            {
                // UIオブジェクトがnullでないか確認
                if (uiData == null || uiData.gameObject == null)
                {
                    m_PlayerUIMap.Remove(playerId);
                    return;
                }

                uiData.UpdateNameText(playerName);
            }
        }

        /// <summary>
        /// オブジェクトの完全なヒエラルキーパスを取得
        /// </summary>
        private string GetHierarchyPath(Transform transform)
        {
            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }
            return path;
        }
    }
}
