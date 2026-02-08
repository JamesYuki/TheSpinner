using System.Collections.Generic;
using PurrNet.Prediction;
using UnityEngine;

namespace Spinner
{
    /// <summary>
    /// テレポートゾーンの管理を行うユーティリティサービス。
    /// シーンに1つ配置し、全TeleportZoneを管理する。
    /// シャッフルのタイミングや状態はPredictedStateNode（RoundRunningState）が管理する。
    /// このクラス自体はNetworkBehaviourではなく、予測ループから呼び出される純粋なサービス。
    /// </summary>
    public class TeleportManager : MonoBehaviour
    {
        [Header("テレポートゾーン")]
        [SerializeField, Tooltip("1Pエリア（左側）のテレポートゾーン配置スロット")]
        private List<Transform> m_LeftSlots = new();

        [SerializeField, Tooltip("2Pエリア（右側）のテレポートゾーン配置スロット")]
        private List<Transform> m_RightSlots = new();

        private readonly List<TeleportZone> m_LeftZones = new();
        private readonly List<TeleportZone> m_RightZones = new();

        private readonly Dictionary<TeleportZone, TeleportZone> m_PairMap = new();

        /// <summary>初期収集時のカラーID（シャッフル計算のベース。変更されない）</summary>
        private TeleportColorId[] m_OriginalLeftColors;
        private TeleportColorId[] m_OriginalRightColors;

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

        private void OnDestroy()
        {
            ServiceLocator.Unregister<TeleportManager>();
        }

        // ────────────────────────────────────────────
        //  初期化
        // ────────────────────────────────────────────

        /// <summary>
        /// テレポートシステムを初期化する。
        /// RoundRunningState.Enter() から呼ばれる。
        /// </summary>
        public void Initialize()
        {
            if (!m_Initialized)
            {
                CollectZones();
                SaveOriginalColors();
                BuildPairMap();
                ApplyVisualColors();
                m_Initialized = true;
            }
        }

        // ────────────────────────────────────────────
        //  シャッフル（予測ループから呼び出し）
        // ────────────────────────────────────────────

        /// <summary>
        /// シード値からシャッフルを適用する。
        /// 予測ループ内から呼ばれる（RoundRunningStateのSetUnityState）。
        /// 初期カラーIDをベースにシャッフルするため、同じシードなら何度呼んでも同じ結果になる（冪等）。
        /// </summary>
        public void ApplyShuffleFromSeed(uint seed)
        {
            if (!m_Initialized) return;

            var random = PredictedRandom.Create(seed);
            var leftColors = CalculateShuffledColors(m_OriginalLeftColors, ref random);
            var rightColors = CalculateShuffledColors(m_OriginalRightColors, ref random);

            ApplyColorIds(m_LeftZones, leftColors, "Left");
            ApplyColorIds(m_RightZones, rightColors, "Right");
            BuildPairMap();
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

        /// <summary>初期カラーIDを保存する（シャッフル計算のベースとして使用）</summary>
        private void SaveOriginalColors()
        {
            m_OriginalLeftColors = new TeleportColorId[m_LeftZones.Count];
            for (int i = 0; i < m_LeftZones.Count; i++)
            {
                m_OriginalLeftColors[i] = m_LeftZones[i].ColorId;
            }

            m_OriginalRightColors = new TeleportColorId[m_RightZones.Count];
            for (int i = 0; i < m_RightZones.Count; i++)
            {
                m_OriginalRightColors[i] = m_RightZones[i].ColorId;
            }
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
        /// 初期カラーID配列をシャッフルした結果を計算する。
        /// Fisher-Yatesアルゴリズムで決定的シャッフル。
        /// PredictedRandomを使用して全クライアントで同じ結果を保証。
        /// 入力のoriginalColorsは変更されない。
        /// </summary>
        private TeleportColorId[] CalculateShuffledColors(TeleportColorId[] originalColors, ref PredictedRandom random)
        {
            int count = originalColors.Length;
            if (count <= 1)
            {
                return (TeleportColorId[])originalColors.Clone();
            }

            var colorIds = (TeleportColorId[])originalColors.Clone();

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
            ApplyShuffleFromSeed(seed);
            AppLogger.Log($"[TeleportManager] エディタテストシャッフル: Seed={seed}");
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
