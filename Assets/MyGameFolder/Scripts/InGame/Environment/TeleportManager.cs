using System;
using System.Collections.Generic;
using PurrNet;
using PurrNet.Prediction;
using PurrNet.Transports;
using UnityEngine;

namespace Spinner
{
    /// <summary>
    /// テレポートゾーンの管理・シャッフル同期を行うマネージャー。
    /// シーンに1つ配置し、全TeleportZoneを管理する。
    /// PurrNetのNetworkBehaviourとして動作し、ObserversRpcでシャッフルを同期する。
    /// </summary>
    public class TeleportManager : NetworkBehaviour
    {
        [Header("テレポートゾーン")]
        [SerializeField, Tooltip("1Pエリア（左側）のテレポートゾーン配置スロット")]
        private List<Transform> m_LeftSlots = new();

        [SerializeField, Tooltip("2Pエリア（右側）のテレポートゾーン配置スロット")]
        private List<Transform> m_RightSlots = new();

        [Header("シャッフル設定")]
        [SerializeField, Tooltip("シャッフル間隔（秒）")]
        private float m_ShuffleIntervalSeconds = 15f;

        [SerializeField, Tooltip("最初のシャッフルまでの遅延（秒）")]
        private float m_InitialDelaySeconds = 10f;

        [SerializeField, Tooltip("シャッフルを有効にする")]
        private bool m_ShuffleEnabled = true;

        private readonly List<TeleportZone> m_LeftZones = new();
        private readonly List<TeleportZone> m_RightZones = new();

        private readonly Dictionary<TeleportZone, TeleportZone> m_PairMap = new();

        private float m_ShuffleTimer;
        private bool m_Initialized;

        // 各色に対応するビジュアルカラー
        private static readonly Dictionary<TeleportColorId, Color> s_ColorMap = new()
        {
            { TeleportColorId.Red, Color.red },
            { TeleportColorId.Blue, new Color(0.2f, 0.4f, 1f) },
            { TeleportColorId.Yellow, Color.yellow },
            { TeleportColorId.Green, Color.green },
            { TeleportColorId.Purple, new Color(0.6f, 0.2f, 0.9f) },
        };

        private void Awake()
        {
            ServiceLocator.Register(this);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            ServiceLocator.Unregister<TeleportManager>();
        }

        /// <summary>
        /// ネットワーク生成後に呼ばれる（サーバー・クライアント両方）
        /// </summary>
        protected override void OnSpawned(bool asServer)
        {
            base.OnSpawned(asServer);

            if (!m_Initialized)
            {
                CollectZones();
                BuildPairMap();
                ApplyVisualColors();
                m_Initialized = true;
            }

            if (asServer)
            {
                m_ShuffleTimer = m_InitialDelaySeconds;
            }
        }

        /// <summary>
        /// Round開始時に呼ばれる初期同期メソッド（公開インターフェース）
        /// State管理から呼び出される
        /// </summary>
        public void InitializeForRound()
        {
            if (!isServer) return;

            if (!m_Initialized)
            {
                CollectZones();
                BuildPairMap();
                ApplyVisualColors();
                m_Initialized = true;
            }

            SyncInitialState();
        }

        /// <summary>
        /// サーバーが初期状態を全クライアントに同期する
        /// </summary>
        private void SyncInitialState()
        {
            if (!isServer) return;

            var leftColors = m_LeftZones.ConvertAll(z => z.ColorId).ToArray();
            var rightColors = m_RightZones.ConvertAll(z => z.ColorId).ToArray();
            uint seed = 0;
            ShuffleZones_ObserversRpc(seed, leftColors, rightColors);
        }

        private void Update()
        {
            if (!isServer || !m_ShuffleEnabled) return;

            m_ShuffleTimer -= Time.deltaTime;
            if (m_ShuffleTimer <= 0f)
            {
                m_ShuffleTimer = m_ShuffleIntervalSeconds;

                uint seed = (uint)UnityEngine.Random.Range(0, int.MaxValue);
                var random = PredictedRandom.Create(seed);
                var leftColors = CalculateShuffledColors(m_LeftZones, ref random);
                var rightColors = CalculateShuffledColors(m_RightZones, ref random);

                ShuffleZones_ObserversRpc(seed, leftColors, rightColors);
            }
        }

        // ────────────────────────────────────────────
        //  ゾーン収集・ペア構築
        // ────────────────────────────────────────────

        /// <summary>シーン内の全 TeleportZone を収集して左右に分類</summary>
        private void CollectZones()
        {
            m_LeftZones.Clear();
            m_RightZones.Clear();

            var allZones = FindObjectsByType<TeleportZone>(FindObjectsSortMode.None);
            foreach (var zone in allZones)
            {
                if (zone.TeamSide == 0)
                    m_LeftZones.Add(zone);
                else
                    m_RightZones.Add(zone);
            }

            // 位置とColorIdで安定した順序にソート（全クライアントで同じ順序を保証）
            m_LeftZones.Sort((a, b) =>
            {
                int colorCompare = a.ColorId.CompareTo(b.ColorId);
                if (colorCompare != 0) return colorCompare;

                // 同じColorIdの場合はZ座標、次にX座標でソート
                int zCompare = a.transform.position.z.CompareTo(b.transform.position.z);
                if (zCompare != 0) return zCompare;

                return a.transform.position.x.CompareTo(b.transform.position.x);
            });

            m_RightZones.Sort((a, b) =>
            {
                int colorCompare = a.ColorId.CompareTo(b.ColorId);
                if (colorCompare != 0) return colorCompare;

                int zCompare = a.transform.position.z.CompareTo(b.transform.position.z);
                if (zCompare != 0) return zCompare;

                return a.transform.position.x.CompareTo(b.transform.position.x);
            });
        }

        /// <summary>同じ ColorId のゾーンをペアとして登録</summary>
        private void BuildPairMap()
        {
            m_PairMap.Clear();

            var colorToLeft = new Dictionary<TeleportColorId, TeleportZone>();
            var colorToRight = new Dictionary<TeleportColorId, TeleportZone>();

            foreach (var zone in m_LeftZones)
            {
                if (colorToLeft.ContainsKey(zone.ColorId))
                    continue;
                colorToLeft[zone.ColorId] = zone;
            }

            foreach (var zone in m_RightZones)
            {
                if (colorToRight.ContainsKey(zone.ColorId))
                    continue;
                colorToRight[zone.ColorId] = zone;
            }

            // ペア構築
            foreach (var kvp in colorToLeft)
            {
                if (colorToRight.TryGetValue(kvp.Key, out var rightZone))
                {
                    m_PairMap[kvp.Value] = rightZone;
                    m_PairMap[rightZone] = kvp.Value;
                }
            }
        }

        /// <summary>各ゾーンにビジュアルカラーを適用</summary>
        private void ApplyVisualColors()
        {
            void ApplyToList(List<TeleportZone> zones)
            {
                foreach (var zone in zones)
                {
                    if (s_ColorMap.TryGetValue(zone.ColorId, out var color))
                    {
                        zone.ApplyVisualColor(color);
                    }
                }
            }

            ApplyToList(m_LeftZones);
            ApplyToList(m_RightZones);
        }

        // ────────────────────────────────────────────
        //  テレポート処理
        // ────────────────────────────────────────────

        /// <summary>
        /// 指定したゾーンのペアとなるゾーンを取得
        /// </summary>
        /// <param name="zone">入口ゾーン</param>
        /// <returns>ペアのゾーン。見つからなければ null</returns>
        public TeleportZone GetPairedZone(TeleportZone zone)
        {
            return m_PairMap.TryGetValue(zone, out var paired) ? paired : null;
        }

        /// <summary>
        /// パックをテレポートさせる。Puck から呼ばれる。
        /// </summary>
        /// <param name="entryZone">パックが入ったゾーン</param>
        /// <param name="puck">テレポートするパック</param>
        /// <returns>テレポートが成功したか</returns>
        public bool TryTeleport(TeleportZone entryZone, Puck puck)
        {
            var exitZone = GetPairedZone(entryZone);
            if (exitZone == null)
                return false;

            float entrySpeed = entryZone.GetEntrySpeed();

            Vector3 exitPos = exitZone.ExitPosition;
            Vector3 exitDir = exitZone.ExitForward;

            AppLogger.Log($"[Teleport] Entry: {entryZone.ColorId}(Side:{entryZone.TeamSide}) Speed={entrySpeed:F2}");
            AppLogger.Log($"[Teleport] Exit: {exitZone.ColorId}(Side:{exitZone.TeamSide}) Pos={exitPos} Dir={exitDir}");
            AppLogger.Log($"[Teleport] ExitDir Length={exitDir.magnitude:F3} (normalized={exitDir.normalized})");

            Vector3 exitVelocity = exitDir.normalized * entrySpeed;
            AppLogger.Log($"[Teleport] ExitVelocity={exitVelocity} (magnitude={exitVelocity.magnitude:F2})");

            puck.Teleport(exitPos, exitVelocity);

            exitZone.RecordTeleport(exitVelocity);

            entryZone.PlayEffect();
            exitZone.PlayEffect();

            return true;
        }

        [ObserversRpc(bufferLast: true, channel: Channel.ReliableOrdered, runLocally: true)]
        private void ShuffleZones_ObserversRpc(uint seed, TeleportColorId[] leftColors, TeleportColorId[] rightColors)
        {
            ApplyShuffle(seed, leftColors, rightColors);
        }

        /// <summary>
        /// シード値を使って左右それぞれのゾーン配置をシャッフルする。
        /// 同じシードなら全クライアントで同じ結果になる。
        /// PredictedRandomを使用して完全な決定性を保証。
        /// </summary>
        private void ApplyShuffle(uint seed, TeleportColorId[] leftColors, TeleportColorId[] rightColors)
        {
            AppLogger.Log($"[TeleportManager] シャッフル実行: Seed={seed}");

            ApplyColorIds(m_LeftZones, leftColors, "Left");
            ApplyColorIds(m_RightZones, rightColors, "Right");
            BuildPairMap();
        }

        /// <summary>
        /// 受信したColorId配列をゾーンに適用する
        /// </summary>
        private void ApplyColorIds(List<TeleportZone> zones, TeleportColorId[] colorIds, string sideName)
        {
            if (zones.Count != colorIds.Length)
            {
                AppLogger.LogError($"[TeleportManager] {sideName} ゾーン数とColorId配列の長さが一致しません: zones={zones.Count}, colors={colorIds.Length}");
                return;
            }

            for (int i = 0; i < zones.Count; i++)
            {
                var zone = zones[i];
                var newColorId = colorIds[i];

                if (s_ColorMap.TryGetValue(newColorId, out var color))
                {
                    zone.SetColorId(newColorId, color);
                    AppLogger.Log($"[TeleportManager] {sideName} Zone{i} -> {newColorId}");
                }
            }
        }

        /// <summary>
        /// ゾーンリストの各ゾーンのColorIdをシャッフルした結果を計算する。
        /// Fisher-Yatesアルゴリズムで決定的シャッフル。
        /// PredictedRandomを使用して全クライアントで同じ結果を保証。
        /// </summary>
        private TeleportColorId[] CalculateShuffledColors(List<TeleportZone> zones, ref PredictedRandom random)
        {
            int count = zones.Count;
            if (count <= 1)
            {
                return zones.ConvertAll(z => z.ColorId).ToArray();
            }

            var colorIds = new TeleportColorId[count];
            for (int i = 0; i < count; i++)
            {
                colorIds[i] = zones[i].ColorId;
            }

            // Fisher-Yatesシャッフル
            for (int i = count - 1; i >= 1; i--)
            {
                int j = random.Next(0, i + 1);
                var temp = colorIds[i];
                colorIds[i] = colorIds[j];
                colorIds[j] = temp;
            }

            return colorIds;
        }

        /// <summary>シャッフルの有効/無効を切り替え</summary>
        public void SetShuffleEnabled(bool enabled)
        {
            m_ShuffleEnabled = enabled;
        }

        /// <summary>シャッフル間隔を変更</summary>
        public void SetShuffleInterval(float seconds)
        {
            m_ShuffleIntervalSeconds = seconds;
        }

        /// <summary>手動でシャッフルを実行（サーバーのみ）</summary>
        public void ForceShuffleFromServer()
        {
            if (!isServer) return;

            uint seed = (uint)UnityEngine.Random.Range(0, int.MaxValue);
            var random = PredictedRandom.Create(seed);

            var leftColors = CalculateShuffledColors(m_LeftZones, ref random);
            var rightColors = CalculateShuffledColors(m_RightZones, ref random);

            ShuffleZones_ObserversRpc(seed, leftColors, rightColors);
        }

#if UNITY_EDITOR
        /// <summary>エディタテスト用: ローカルでシャッフルを実行</summary>
        [ContextMenu("Test Shuffle (Editor Only)")]
        public void TestShuffleInEditor()
        {
            if (!Application.isPlaying) return;

            if (!m_Initialized)
            {
                CollectZones();
                BuildPairMap();
                ApplyVisualColors();
                m_Initialized = true;
            }

            uint seed = (uint)UnityEngine.Random.Range(0, int.MaxValue);
            var random = PredictedRandom.Create(seed);

            var leftColors = CalculateShuffledColors(m_LeftZones, ref random);
            var rightColors = CalculateShuffledColors(m_RightZones, ref random);

            AppLogger.Log($"[TeleportManager] エディタテストシャッフル開始");
            ApplyShuffle(seed, leftColors, rightColors);
        }
#endif

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // スロット位置を表示
            DrawSlotGizmos(m_LeftSlots, Color.cyan, "L");
            DrawSlotGizmos(m_RightSlots, Color.magenta, "R");
        }

        private void DrawSlotGizmos(List<Transform> slots, Color color, string prefix)
        {
            if (slots == null) return;

            Gizmos.color = color;
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i] == null) continue;
                Gizmos.DrawWireCube(slots[i].position, Vector3.one * 0.8f);

                UnityEditor.Handles.Label(
                    slots[i].position + Vector3.up * 0.5f,
                    $"{prefix}{i}",
                    new GUIStyle
                    {
                        normal = new GUIStyleState { textColor = color },
                        fontSize = 11,
                        fontStyle = FontStyle.Bold
                    }
                );
            }
        }
#endif
    }
}
