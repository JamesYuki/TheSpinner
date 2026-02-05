# PurrNet 通信システム総合ドキュメント

## 目次
1. [予測＆精度システム](#予測精度システム)
   - [FloatAccuracy - ネットワーク精度設定](#floataccuracy---ネットワーク精度設定)
   - [Unparent Graphics - グラフィック分離](#unparent-graphics---グラフィック分離)
   - [CharacterController Patch](#charactercontroller-patch)
2. [ラグ補償＆スムージング](#ラグ補償スムージング)
   - [ColliderRollback - サーバーサイドラグ補償](#colliderrollback---サーバーサイドラグ補償)
   - [TransformInterpolation - スムーズ補正](#transforminterpolation---スムーズ補正)
3. [通信システム](#通信システム)
   - [RPC (Remote Procedure Call)](#rpc-remote-procedure-call)
   - [SyncVar - 同期変数](#syncvar---同期変数)
   - [SyncList - 同期リスト](#synclist---同期リスト)
   - [Channel - 配信方法](#channel---配信方法)
   - [DeltaModule - 差分圧縮](#deltamodule---差分圧縮)
   - [TickManager - ティックレート管理](#tickmanager---ティックレート管理)
4. [最適化システム](#最適化システム)
   - [NetworkVisibility - 関心範囲管理](#networkvisibility---関心範囲管理)
   - [StatisticsManager - パフォーマンス監視](#statisticsmanager---パフォーマンス監視)
5. [総合活用ガイド](#総合活用ガイド)

---

## 予測＆精度システム

### FloatAccuracy - ネットワーク精度設定

ネットワーク送信時のデータサイズと精度のトレードオフを調整するパラメータ。

#### 設定レベル

| レベル | 精度 | データサイズ | 用途 |
|--------|------|-------------|------|
| **Purrfect (0)** | 32ビット float | 最大 | 物理演算など高精度が必要な場合 |
| **Medium (1)** ⭐ | 0.001精度 (CompressedFloat) | 中間 | **推奨デフォルト設定** |
| **Low (2)** | 16ビット half | 最小 | 帯域幅が極端に制限される場合 |

#### Medium の仕組み
```
例: 1.23456 → 1235 (整数に変換) → 1.235 (復元後)
float値を1000倍して整数で送信、受信側で1000で割る
```

#### 影響を受けるコンポーネント
- **PredictedTransform**: Position (Vector3), Rotation (Quaternion)
- **PredictedRigidbody**: LinearVelocity, AngularVelocity

---

### Unparent Graphics - グラフィック分離

#### 概要
`_unparentGraphics` は、予測システム動作中にグラフィックオブジェクトを親から切り離すオプション。

#### 目的
予測の再計算 (Reconcile) 中にGameObjectが頻繁にenable/disableされる場合、グラフィックオブジェクトを別管理することで表示の問題を回避。

#### 実装詳細
```csharp
protected override void LateAwake()
{
    if (_hasView && _unparentGraphics)
    {
        _originalGraphicsParent = _graphics.parent;  // 元の親を記憶
        _graphics.SetParent(null);                    // 親から切り離す
    }
}
```

#### 使用ケース
- オブジェクトプーリング使用時
- 予測補正時にGameObjectのenable/disableが頻繁に切り替わる場合
- グラフィック表示を安定化させたい場合

**現在のプロジェクト設定:** `false` (無効)

---

### CharacterController Patch

#### 概要
Unity の CharacterController コンポーネント使用時に、位置設定を正しく行うためのパッチ。

#### 問題
CharacterController が有効な状態では `transform.position` への直接代入が正しく機能しないことがある。

#### 解決策
```csharp
protected override void SetUnityState(PredictedTransformState state)
{
    if (_characterControllerPatch && _hasController)
    {
        _unityCtrler.enabled = false;                           // 一時的に無効化
        transform.SetPositionAndRotation(state.unityPosition, 
                                        state.unityRotation);   // 位置設定
        _unityCtrler.enabled = true;                            // 再有効化
        return;
    }
    
    transform.SetPositionAndRotation(state.unityPosition, state.unityRotation);
}
```

#### ワークフロー
1. CharacterController を一時的に無効化
2. Transform の位置・回転を設定
3. CharacterController を再有効化

#### パフォーマンス影響
- フレーム毎のenable/disable切り替えによる軽微なオーバーヘッド
- CharacterController不使用時は無効にすべき

**現在のプロジェクト設定:**
- PlayerController: `false` (Rigidbody使用のため不要)
- PlayerCore: `true`

---

## ラグ補償＆スムージング

### ColliderRollback - サーバーサイドラグ補償

#### 概要
PurrNet のサーバーサイドラグ補償システム。クライアントの視点の時間で当たり判定を行うことで、ネットワーク遅延を補償。

#### 解決する問題
オンラインゲームでは、クライアントが武器を発射したタイミングとサーバーが判定するタイミングに遅延があります。ラグ補償なしでは、サーバーは「現在」のオブジェクト位置で判定するため、「壁に隠れたのに撃たれた」などの不公平が発生します。

#### 動作原理

**コンポーネント設定:**
```csharp
// ColliderRollback.cs - ラグ補償が必要なオブジェクトにアタッチ
[SerializeField] private float _storeHistoryInSeconds = 5f;  // 履歴保存期間
[SerializeField] private bool _autoAddAllChildren = true;     // 子コライダー自動登録
[SerializeField] private Collider[] _colliders3D;             // 手動登録する3Dコライダー
[SerializeField] private Collider2D[] _colliders2D;           // 手動登録する2Dコライダー
```

**履歴ストレージ:**
- 指定期間（デフォルト5秒）分のコライダー位置・回転を毎ティック保存
- `SimpleHistory<Collider3DState>` と `SimpleHistory<Collider2DState>` データ構造使用
- ティック間を自動的に補間して正確なタイミングを再現

**サーバー側の使用例:**
```csharp
// クライアントの視点時間で判定
var clientTick = playerConnection.GetClientTick();  // クライアントの視点時間取得
RollbackModule rollbackModule = NetworkManager.main.rollbackModule;

// 過去の時刻でレイキャスト実行
if (rollbackModule.SphereCast(clientTick, origin, radius, direction, out hit, maxDistance))
{
    // クライアントのラグを補償した公平な判定！
    Debug.Log($"Hit {hit.collider.name} at historical time");
}
```

**利用可能なメソッド:**
```csharp
// RollbackModule3D.cs - 3D衝突クエリ
bool SphereCast(double preciseTick, Vector3 origin, float radius, Vector3 direction, 
                out RaycastHit hitInfo, float maxDistance, int layerMask);
                
bool BoxCast(double preciseTick, Vector3 center, Vector3 halfExtents, Vector3 direction,
             out RaycastHit hitInfo, Quaternion orientation, float maxDistance, int layerMask);
             
bool CapsuleCast(double preciseTick, Vector3 point1, Vector3 point2, float radius,
                 Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layerMask);

int SphereOverlapNonAlloc(double preciseTick, Vector3 position, float radius,
                          Collider[] results, int layerMask);

// 状態クエリ
bool TryGetColliderState(double preciseTick, Collider collider, out Collider3DState state);
```

#### 使用ケース
- **武器のヒット判定**: クライアントの射撃をその視点時間で検証
- **近接攻撃**: ラグを考慮した公平な近接戦闘
- **範囲ダメージ**: 爆発・AOEダメージを特定の過去時点で判定
- **アンチチート検証**: クライアントの報告した行動が実際に可能だったか検証

#### パフォーマンス考慮事項
- メモリ使用量: コライダーあたり約5秒分の履歴
- 各登録コライダーはティック毎（通常30-60回/秒）に位置・回転を保存
- レイヤーマスクを使用して衝突クエリを制限
- メモリが制約される場合は `_storeHistoryInSeconds` を削減

---

### TransformInterpolation - スムーズ補正

#### 概要
予測エラーが発生した際、瞬間移動ではなく時間をかけて滑らかに補正するシステム。

#### 設定

**ScriptableObject設定:**
```csharp
// TransformInterpolationSettings.cs
public class TransformInterpolationSettings : ScriptableObject
{
    [SerializeField] private bool useInterpolation = true;
    [SerializeField] private PredictedInterpolation positionInterpolation;
    [SerializeField] private PredictedInterpolation rotationInterpolation;
}
```

**PredictedInterpolation 構造:**
```csharp
[Serializable]
public struct PredictedInterpolation
{
    // エラー量に基づく補正速度カーブ
    [SerializeField] private Vector2 correctionRateMinMax;
    
    // エラー量に基づくブレンド量カーブ
    [SerializeField] private Vector2 correctionBlendMinMax;
    
    // テレポート閾値 - これを超えると瞬間移動
    [SerializeField] private Vector2 teleportThresholdMinMax;
}
```

#### デフォルト値

**位置補正:**
- `correctionRateMinMax`: (3.3, 10.0)
  - 小さいエラー: 毎秒3.3ユニットで補正
  - 大きいエラー: 毎秒10.0ユニットで補正
- `correctionBlendMinMax`: (0.25, 4.0)
  - エラー量に応じてブレンド係数増加
- `teleportThresholdMinMax`: (0.025, 5.0)
  - 0.025ユニット未満: 無視
  - 5.0ユニット超: 瞬間移動

**回転補正:**
- `correctionRateMinMax`: (3.3, 10.0) 度/秒
- `correctionBlendMinMax`: (5.0, 30.0)
- `teleportThresholdMinMax`: (1.5, 52.0) 度

#### 動作原理

```csharp
// スムーズ補正の疑似コード
float errorMagnitude = Vector3.Distance(predictedPos, authorityPos);

if (errorMagnitude > teleportThreshold.y)
{
    // エラーが大きすぎる - 瞬間移動
    transform.position = authorityPos;
}
else if (errorMagnitude > teleportThreshold.x)
{
    // スムーズ補正
    float t = Mathf.InverseLerp(teleportThreshold.x, teleportThreshold.y, errorMagnitude);
    float correctionRate = Mathf.Lerp(correctionRateMinMax.x, correctionRateMinMax.y, t);
    float blend = Mathf.Lerp(correctionBlendMinMax.x, correctionBlendMinMax.y, t);
    
    transform.position = Vector3.Lerp(transform.position, authorityPos, 
                                      blend * correctionRate * Time.deltaTime);
}
// else: エラーが小さすぎる、無視
```

#### 使用ケース
- **キャラクター移動**: 移動中の予測エラーを滑らかに補正
- **高速移動オブジェクト**: 弾丸などのビジュアルジッター防止
- **ネットワーク不安定時**: パケットロスを優雅に処理
- **フィーリング調整**: レスポンス性 vs スムーズさのバランス調整

#### チューニングガイドライン

**レスポンス重視のゲームプレイ（FPSなど）:**
- `correctionRateMinMax` を増加（速い補正）
- `teleportThresholdMinMax.y` を減少（早めにテレポート）
- ブレンド値は中程度に維持

**スムーズなビジュアル重視（三人称視点など）:**
- `correctionRateMinMax` を減少（遅く、滑らか）
- `teleportThresholdMinMax` を増加（大きなエラーも許容）
- ブレンド値を増加

**物理オブジェクト:**
- 物理エラー回避のため高めのテレポート閾値
- ジッター防止のため中程度の補正レート

---

## 通信システム

### RPC (Remote Procedure Call)

#### 概要
ネットワーク越しにメソッドを呼び出すシステム。PurrNetは3種類のRPCタイプを提供。

#### RPCタイプ

##### 1. ServerRPC - クライアント→サーバー
クライアントがサーバー上のメソッドを呼び出す。

```csharp
[ServerRpc]
private void RequestSpawnItem(ItemType itemType)
{
    // サーバー上で実行される
    // クライアントからのリクエストに応答
}
```

**用途:**
- プレイヤーアクション送信（攻撃、アイテム使用など）
- サーバーへのリクエスト送信
- 入力コマンド送信

##### 2. ObserversRPC - サーバー→全観察者
サーバーが全ての観察者（クライアント）に対してメソッドを呼び出す。

```csharp
[ObserversRpc]
private void SpawnEffect(Vector3 position, EffectType type)
{
    // 全クライアントで実行される
    Instantiate(effectPrefab, position, Quaternion.identity);
}
```

**用途:**
- エフェクト再生
- サウンド再生
- 全プレイヤーへのイベント通知

##### 3. TargetRPC - サーバー→特定プレイヤー
サーバーが特定のプレイヤーに対してメソッドを呼び出す。

```csharp
[TargetRpc]
private void ShowNotification(string message)
{
    // 特定のクライアントでのみ実行
    UIManager.Instance.ShowMessage(message);
}
```

**用途:**
- 個別プレイヤーへの通知
- プライベート情報の送信
- UI更新

#### RPC属性パラメータ

```csharp
[ServerRpc(
    channel: Channel.ReliableOrdered,        // 配信方法
    runLocally: false,                       // ローカルでも実行するか
    requireOwnership: true,                  // オーナーシップ要求
    bufferLast: false,                       // 最後の呼び出しをバッファ
    requireServer: false,                    // サーバー要求
    excludeOwner: false,                     // オーナーを除外
    excludeSender: false                     // 送信者を除外
)]
```

**主要パラメータ:**
- **channel**: 配信方法（後述）
- **runLocally**: true の場合、送信元でも即座に実行
- **requireOwnership**: true の場合、オーナーのみ呼び出し可能
- **bufferLast**: 新規参加者に最後の呼び出しを再送
- **excludeOwner**: ObserversRPC でオーナーを除外
- **compressionLevel**: 圧縮レベル設定
- **deltaPacked**: 差分圧縮を使用

#### 使用例

```csharp
public class PlayerController : NetworkBehaviour
{
    // クライアント→サーバー: 攻撃リクエスト
    [ServerRpc]
    private void Attack_ServerRpc(Vector3 targetPosition)
    {
        // サーバーでダメージ計算
        var hit = Physics.Raycast(transform.position, targetPosition - transform.position, out var hitInfo);
        if (hit)
        {
            // 全クライアントにエフェクト表示
            ShowAttackEffect_ObserversRpc(hitInfo.point);
            
            // ヒットしたプレイヤーに通知
            if (hitInfo.collider.TryGetComponent<PlayerController>(out var target))
            {
                target.TakeDamage_TargetRpc(10);
            }
        }
    }
    
    // サーバー→全観察者: エフェクト表示
    [ObserversRpc]
    private void ShowAttackEffect_ObserversRpc(Vector3 position)
    {
        Instantiate(attackEffectPrefab, position, Quaternion.identity);
    }
    
    // サーバー→特定プレイヤー: ダメージ通知
    [TargetRpc]
    private void TakeDamage_TargetRpc(int damage)
    {
        health -= damage;
        UIManager.Instance.ShowDamageUI(damage);
    }
}
```

---

### SyncVar - 同期変数

#### 概要
ネットワーク越しで自動的に同期される変数。値が変更されると自動的に全クライアントに送信される。

#### 基本使用法

```csharp
public class PlayerStats : NetworkBehaviour
{
    [SerializeField]
    private SyncVar<int> health = new SyncVar<int>
    {
        value = 100,
        ownerAuth = false,           // false = サーバー権限
        sendIntervalInSeconds = 0    // 0 = 即座に送信
    };
    
    private void Awake()
    {
        // 値変更イベント登録
        health.onChanged += OnHealthChanged;
    }
    
    private void OnHealthChanged(int newHealth)
    {
        Debug.Log($"Health changed to {newHealth}");
        UpdateHealthUI(newHealth);
    }
    
    public void TakeDamage(int damage)
    {
        if (isServer)
        {
            // サーバーが値を変更すると自動的に全クライアントに同期
            health.value -= damage;
        }
    }
}
```

#### 主要プロパティ

| プロパティ | 説明 |
|-----------|------|
| `value` | 同期される値。変更時に自動送信 |
| `ownerAuth` | true = オーナーのみ変更可能、false = サーバーのみ変更可能 |
| `sendIntervalInSeconds` | 送信間隔（秒）。0 = 即座、> 0 = 間隔を空けて送信 |
| `onChanged` | 値変更時に呼ばれるイベント |
| `onChangedWithOld` | 古い値と新しい値の両方を受け取るイベント |

#### 権限モード

**サーバー権限 (ownerAuth = false):**
```csharp
private SyncVar<int> score = new SyncVar<int> { ownerAuth = false };

// サーバーのみ変更可能
if (isServer)
{
    score.value += 10;  // OK
}
```

**オーナー権限 (ownerAuth = true):**
```csharp
private SyncVar<Vector3> position = new SyncVar<Vector3> { ownerAuth = true };

// オーナークライアントが変更可能
if (isOwner)
{
    position.value = transform.position;  // OK
}
```

#### 送信間隔の最適化

```csharp
// 即座に送信（重要な値）
private SyncVar<int> health = new SyncVar<int> 
{ 
    sendIntervalInSeconds = 0  // 毎回即座に送信
};

// 間隔を空けて送信（頻繁に変わるが重要度が低い値）
private SyncVar<float> temperature = new SyncVar<float>
{
    sendIntervalInSeconds = 1.0f  // 1秒間隔で送信
};
```

#### 使用例

```csharp
public class NetworkedDoor : NetworkBehaviour
{
    [SerializeField]
    private SyncVar<bool> isOpen = new SyncVar<bool> 
    { 
        ownerAuth = false,           // サーバー権限
        sendIntervalInSeconds = 0    // 即座に同期
    };
    
    private Animator animator;
    
    private void Awake()
    {
        animator = GetComponent<Animator>();
        
        // 変更イベントに登録
        isOpen.onChanged += OnDoorStateChanged;
    }
    
    private void OnDoorStateChanged(bool newState)
    {
        // 全クライアントでアニメーション再生
        animator.SetBool("IsOpen", newState);
    }
    
    public void ToggleDoor()
    {
        if (isServer)
        {
            // サーバーが状態変更すると自動的に全クライアントに同期
            isOpen.value = !isOpen.value;
        }
    }
}
```

#### パフォーマンス考慮事項
- **送信間隔**: 頻繁に変わる値は `sendIntervalInSeconds` を設定して帯域幅節約
- **データサイズ**: 大きな構造体は避ける。必要に応じてRPCを使用
- **変更頻度**: フレーム毎に変わる値には不向き（PredictedTransform使用を検討）

---

### SyncList - 同期リスト

#### 概要
ネットワーク越しで自動的に同期されるリスト型コレクション。リストの追加・削除・変更操作が自動的に全クライアントに送信される。

#### 基本使用法

```csharp
public class Inventory : NetworkBehaviour
{
    [SerializeField]
    private SyncList<ItemData> items = new SyncList<ItemData>
    {
        ownerAuth = false,           // false = サーバー権限
        sendIntervalInSeconds = 0    // 0 = 即座に送信
    };
    
    private void Awake()
    {
        // リスト変更イベント登録
        items.onChanged += OnInventoryChanged;
    }
    
    private void OnInventoryChanged(SyncListChange<ItemData> change)
    {
        switch (change.operation)
        {
            case SyncListOperation.Added:
                Debug.Log($"アイテム追加: {change.value} at index {change.index}");
                UpdateInventoryUI();
                break;
                
            case SyncListOperation.Removed:
                Debug.Log($"アイテム削除: {change.oldValue} from index {change.index}");
                UpdateInventoryUI();
                break;
                
            case SyncListOperation.Set:
                Debug.Log($"アイテム変更: {change.oldValue} → {change.value} at index {change.index}");
                UpdateInventorySlot(change.index);
                break;
                
            case SyncListOperation.Insert:
                Debug.Log($"アイテム挿入: {change.value} at index {change.index}");
                UpdateInventoryUI();
                break;
                
            case SyncListOperation.Cleared:
                Debug.Log("インベントリクリア");
                ClearInventoryUI();
                break;
        }
    }
    
    public void AddItem(ItemData item)
    {
        if (isServer)
        {
            // サーバーがリストに追加すると自動的に全クライアントに同期
            items.Add(item);
        }
    }
    
    public void RemoveItemAt(int index)
    {
        if (isServer)
        {
            items.RemoveAt(index);
        }
    }
}
```

#### SyncListOperation の種類

| 操作 | 説明 | 発生タイミング |
|------|------|--------------|
| **Added** | リスト末尾に追加 | `list.Add(item)` |
| **Removed** | 要素を削除 | `list.Remove(item)`, `list.RemoveAt(index)` |
| **Insert** | 指定位置に挿入 | `list.Insert(index, item)` |
| **Set** | 既存要素を変更 | `list[index] = newValue` |
| **Cleared** | リスト全消去 | `list.Clear()` |

#### SyncListChange データ構造

```csharp
public readonly struct SyncListChange<T>
{
    public readonly SyncListOperation operation;  // 操作の種類
    public readonly T value;                      // 新しい値
    public readonly T oldValue;                   // 古い値（Set, Removedのみ）
    public readonly int index;                    // 操作対象のインデックス
}
```

#### 実践例：プレイヤーリスト管理

```csharp
public class LobbyManager : NetworkBehaviour
{
    [Serializable]
    public struct PlayerInfo
    {
        public string playerName;
        public int level;
        public bool isReady;
    }
    
    [SerializeField]
    private SyncList<PlayerInfo> lobbyPlayers = new SyncList<PlayerInfo> 
    { 
        ownerAuth = false,           // サーバー管理
        sendIntervalInSeconds = 0    // 即座に同期
    };
    
    private void Awake()
    {
        lobbyPlayers.onChanged += OnLobbyPlayersChanged;
    }
    
    private void OnLobbyPlayersChanged(SyncListChange<PlayerInfo> change)
    {
        // ロビーUIを更新
        UpdateLobbyUI();
        
        // 全プレイヤーが準備完了かチェック
        if (change.operation == SyncListOperation.Set && change.value.isReady)
        {
            CheckAllPlayersReady();
        }
    }
    
    // サーバー：プレイヤー参加
    public void OnPlayerJoined(string playerName)
    {
        if (isServer)
        {
            var newPlayer = new PlayerInfo
            {
                playerName = playerName,
                level = 1,
                isReady = false
            };
            lobbyPlayers.Add(newPlayer);
        }
    }
    
    // サーバー：準備状態切り替え
    public void ToggleReady(int playerIndex)
    {
        if (isServer && playerIndex < lobbyPlayers.Count)
        {
            var player = lobbyPlayers[playerIndex];
            player.isReady = !player.isReady;
            lobbyPlayers[playerIndex] = player;  // Setイベント発火
        }
    }
    
    private void CheckAllPlayersReady()
    {
        bool allReady = lobbyPlayers.Count > 0 && lobbyPlayers.All(p => p.isReady);
        if (allReady)
        {
            StartGame();
        }
    }
}
```

#### List操作のAPI

```csharp
// 要素追加
items.Add(newItem);                    // 末尾に追加 (Added)
items.Insert(2, newItem);              // インデックス2に挿入 (Insert)

// 要素削除
items.Remove(item);                    // 値で削除 (Removed)
items.RemoveAt(3);                     // インデックスで削除 (Removed)
items.Clear();                         // 全削除 (Cleared)

// 要素変更
items[0] = modifiedItem;               // インデックスで設定 (Set)

// 要素アクセス
var item = items[0];                   // インデックスアクセス
int count = items.Count;               // 要素数
bool contains = items.Contains(item);  // 含まれているか

// 列挙
foreach (var item in items)
{
    Debug.Log(item);
}

// Listへの変換
List<ItemData> standardList = items.ToList();
```

#### 権限モード

**サーバー権限 (ownerAuth = false):**
```csharp
private SyncList<string> chatMessages = new SyncList<string> { ownerAuth = false };

// サーバーのみ変更可能
if (isServer)
{
    chatMessages.Add("新しいメッセージ");  // OK
}
```

**オーナー権限 (ownerAuth = true):**
```csharp
private SyncList<Vector3> waypoints = new SyncList<Vector3> { ownerAuth = true };

// オーナークライアントが変更可能
if (isOwner)
{
    waypoints.Add(targetPosition);  // OK
}
```

#### パフォーマンス考慮事項

**送信最適化:**
```csharp
// 複数の変更をまとめて行う場合
if (isServer)
{
    // 悪い例：毎回同期
    for (int i = 0; i < 100; i++)
    {
        items.Add(newItems[i]);  // 100回の同期メッセージ送信
    }
    
    // 良い例：送信間隔設定
    items.sendIntervalInSeconds = 0.1f;  // 100ms毎にまとめて送信
    for (int i = 0; i < 100; i++)
    {
        items.Add(newItems[i]);  // 1回にまとめて送信
    }
}
```

**メモリ使用量:**
- 大きな構造体のリストは避ける
- 必要最小限のデータのみ保持
- 頻繁にClear/Addするより、SetでGC削減

**使用ガイドライン:**
- 小〜中規模のコレクション（〜100要素程度）
- 頻繁に変わらないデータ
- 全クライアントが知る必要のあるリスト
- 例：ロビープレイヤーリスト、インベントリ、スコアボード

---

### Channel - 配信方法

#### 概要
ネットワークパケットの配信方法を指定。信頼性と順序保証のトレードオフを調整。

#### 配信方法の種類

```csharp
public enum Channel : byte
{
    ReliableUnordered,    // 信頼性あり、順序なし
    UnreliableSequenced,  // 信頼性なし、順序あり
    ReliableOrdered,      // 信頼性あり、順序あり
    Unreliable            // 信頼性なし、順序なし
}
```

#### 詳細比較

| Channel | 到達保証 | 順序保証 | 速度 | 用途 |
|---------|---------|---------|------|------|
| **ReliableOrdered** | ✅ | ✅ | 遅い | 重要なイベント、状態変更 |
| **ReliableUnordered** | ✅ | ❌ | 中間 | 順序が重要でない重要データ |
| **UnreliableSequenced** | ❌ | ✅ | 速い | 頻繁な更新（位置など） |
| **Unreliable** | ❌ | ❌ | 最速 | 高頻度・低重要度データ |

#### 配信方法の選択ガイド

**ReliableOrdered（信頼性あり、順序あり）- デフォルト推奨**
```csharp
[ServerRpc(channel: Channel.ReliableOrdered)]
private void SpawnItem(ItemType type, Vector3 position)
{
    // アイテムスポーンは必ず到達し、順序通りに処理されるべき
}
```
- ✅ 使用場面: ゲーム状態変更、スポーン/デスポーン、UI更新
- ❌ 避ける場面: 高頻度の更新（位置同期など）

**ReliableUnordered（信頼性あり、順序なし）**
```csharp
[ObserversRpc(channel: Channel.ReliableUnordered)]
private void PlaySound(SoundType sound)
{
    // サウンド再生順序は重要でないが、確実に再生したい
}
```
- ✅ 使用場面: サウンド再生、パーティクルエフェクト、独立したイベント
- ❌ 避ける場面: 依存関係のあるイベント列

**UnreliableSequenced（信頼性なし、順序あり）**
```csharp
[ServerRpc(channel: Channel.UnreliableSequenced)]
private void SendInputState(Vector2 moveInput, Vector2 lookInput)
{
    // 入力は高頻度更新、古い入力は破棄してOK
}
```
- ✅ 使用場面: プレイヤー入力、位置更新、視線方向
- ❌ 避ける場面: 失っては困ない重要データ

**Unreliable（信頼性なし、順序なし）**
```csharp
[ObserversRpc(channel: Channel.Unreliable)]
private void UpdateDebugInfo(float fps, int ping)
{
    // デバッグ情報は失われても問題ない
}
```
- ✅ 使用場面: デバッグ情報、アナリティクス、非常に高頻度の更新
- ❌ 避ける場面: ゲームプレイに影響する全てのデータ

#### 実践的な使用例

```csharp
public class NetworkedWeapon : NetworkBehaviour
{
    // 弾丸発射: 信頼性あり、順序あり（重要なゲームイベント）
    [ServerRpc(channel: Channel.ReliableOrdered)]
    private void Fire_ServerRpc(Vector3 direction)
    {
        // サーバーで弾丸スポーン
        var bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        bullet.GetComponent<Rigidbody>().velocity = direction * bulletSpeed;
        
        // エフェクト表示（順序不要）
        ShowMuzzleFlash_ObserversRpc();
    }
    
    // マズルフラッシュ: 信頼性あり、順序なし（重要だが順序は不要）
    [ObserversRpc(channel: Channel.ReliableUnordered)]
    private void ShowMuzzleFlash_ObserversRpc()
    {
        muzzleFlashEffect.Play();
    }
    
    // エイミング方向: 信頼性なし、順序あり（高頻度更新）
    [ObserversRpc(channel: Channel.UnreliableSequenced)]
    private void UpdateAimDirection_ObserversRpc(Vector3 direction)
    {
        weaponTransform.forward = direction;
    }
}
```

#### パケットサイズ制限

各トランスポートとChannelには最大パケットサイズがあります:

```csharp
// UDP Transport例
Channel.Unreliable => 1024 bytes
Channel.UnreliableSequenced => 16384 bytes
Channel.ReliableUnordered => 16384 bytes
Channel.ReliableOrdered => 16384 bytes
```

大きなデータは複数のRPC呼び出しに分割するか、ReliableOrderedを使用して自動分割させます。

---

### DeltaModule - 差分圧縮

#### 概要
送信済みの値と新しい値の差分のみを送信することで、帯域幅を大幅に削減するシステム。

#### 動作原理

従来の同期方法:
```
フレーム1: Position(10, 20, 30) → 送信 12 bytes
フレーム2: Position(10, 20, 31) → 送信 12 bytes
フレーム3: Position(10, 20, 32) → 送信 12 bytes
合計: 36 bytes
```

差分圧縮:
```
フレーム1: Position(10, 20, 30) → 送信 12 bytes (フル)
フレーム2: Position(10, 20, 31) → 送信 1 bit (Z軸のみ変更) + 4 bytes
フレーム3: Position(10, 20, 32) → 送信 1 bit + 4 bytes
合計: 約21 bytes (約42%削減)
```

#### 使用方法

**PredictedTransform/PredictedRigidbodyでの自動使用:**
```csharp
protected override bool WriteDeltaState(PlayerID target, BitPacker packer, DeltaModule deltaModule)
{
    switch (_floatAccuracy)
    {
        case FloatAccuracy.Medium:
            var key = new DeltaKey<PredictedTransformCompressedState>(sceneId, id);
            // DeltaModuleが前回の状態と比較して差分のみ送信
            return deltaModule.WriteReliable(packer, target, key, 
                new PredictedTransformCompressedState(currentState));
    }
}
```

**カスタムデータでの使用:**
```csharp
public struct PlayerData : IPackable
{
    public int health;
    public Vector3 position;
    public Quaternion rotation;
    
    // IPack実装
    public void Pack(BitPacker packer) { /* ... */ }
    public void Unpack(BitPacker packer) { /* ... */ }
}

// 差分送信
var key = new DeltaKey<PlayerData>(sceneId, customId);
deltaModule.WriteReliable(packer, targetPlayer, key, newPlayerData);
```

#### 差分圧縮の仕組み

1. **ベースライン保存**: 各クライアントに送信した最後の値を保存
2. **差分検出**: 新しい値とベースラインを比較
3. **変更ビットマスク**: どのフィールドが変更されたかビットで記録
4. **変更値のみ送信**: 変更されたフィールドのみパックして送信
5. **Acknowledgement**: クライアントが受信確認を送り、ベースライン更新

#### 効果的な使用場面

**高頻度更新される構造体:**
```csharp
public struct TransformState
{
    public Vector3 position;    // 毎フレーム少しずつ変化
    public Quaternion rotation; // あまり変化しない
    public Vector3 velocity;    // 頻繁に変化
}
```
- Position の一部の軸だけ送信
- Rotation が変化していなければスキップ
- Velocity の変更分のみ送信

**適していない場面:**
- 毎回全く異なる値（ランダムデータなど）
- 小さなデータ（boolやbyte単体など）- ビットマスクのオーバーヘッドで逆効果
- 一度だけ送信するデータ

#### パフォーマンス指標

実測値（PredictedTransform使用時）:
- 静止時: 約90%削減（位置変化なし）
- 等速移動時: 約30-50%削減（一部の軸のみ変化）
- ランダム移動時: 約10-20%削減（全軸変化）

#### DeltaModule の内部動作

```csharp
public class DeltaModule
{
    // プレイヤー毎にトラッカー管理
    private Dictionary<PlayerID, Dictionary<uint, ClientDeltaTracker>> _sendingTrackers;
    
    // 差分送信
    public bool WriteReliable<T>(BitPacker packer, PlayerID target, DeltaKey<T> key, T newValue)
    {
        var tracker = GetTracker(target, key.uniqueId, true);
        
        // 前回の値と比較
        if (tracker.TryGetLastValue<T>(out var oldValue))
        {
            // 差分のみを書き込み
            return DeltaPacker<T>.WriteDelta(packer, oldValue, newValue);
        }
        else
        {
            // 初回はフルデータ送信
            BitPacker.Pack(packer, newValue);
            tracker.StoreValue(newValue);
            return true;
        }
    }
}
```

---

### TickManager - ティックレート管理

#### 概要
ネットワークシミュレーションのティックレートを管理するシステム。固定時間間隔でゲームロジックを実行。

#### 基本概念

**ティック (Tick)** とは、ネットワークゲームの「シミュレーション刻み」のこと:
```
ティックレート 30Hz の場合:
1秒間に30回シミュレーション = 33.33ms毎に1ティック
```

#### TickManager の役割

```csharp
public class TickManager
{
    public int tickRate { get; private set; }        // ティックレート (Hz)
    public uint currentTick { get; private set; }    // 現在のティック番号
    public float tickDelta { get; private set; }     // ティック間の時間 (秒)
    public double tickDeltaDouble { get; private set; }
    
    // 時間とティックの変換
    public float TickToTime(uint tick);
    public double PreciseTickToTime(double preciseTick);
    public uint TimeToTick(float time);
    public double TimeToDoubleTick(float time);
}
```

#### ティックレートの影響

| ティックレート | ティック間隔 | 長所 | 短所 |
|--------------|------------|------|------|
| **20 Hz** | 50ms | 低CPU負荷、低帯域幅 | 反応が鈍い |
| **30 Hz** | 33ms | バランスが良い ⭐推奨 | - |
| **60 Hz** | 16ms | 高精度、高レスポンス | 高CPU負荷、高帯域幅 |
| **120 Hz** | 8ms | 競技向け最高精度 | 非常に高負荷 |

#### 使用例

**ティック同期処理:**
```csharp
public class NetworkedObject : NetworkBehaviour
{
    protected override void NetworkFixedUpdate()
    {
        // ティック毎に呼ばれる（例: 30回/秒）
        var currentTick = networkManager.tickModule.currentTick;
        
        // ティックベースのシミュレーション
        SimulatePhysics(tickDelta);
        
        // 状態を記録（Rollback用）
        RecordState(currentTick);
    }
}
```

**ティックから時間への変換:**
```csharp
// Rollbackで過去の時刻を計算
var clientTick = playerConnection.GetClientTick();  // 例: 1000
var timeInSeconds = tickManager.TickToTime(clientTick);  // 例: 33.33秒

// Rollbackモジュールで使用
rollbackModule.SphereCast(clientTick, origin, radius, direction, out hit);
```

**時間からティックへの変換:**
```csharp
// 3秒前のティックを取得
float timeAgo = 3.0f;
var currentTick = tickManager.currentTick;
var ticksAgo = tickManager.TimeToTick(timeAgo);  // 例: 90ティック (30Hz * 3秒)
var pastTick = currentTick - ticksAgo;
```

#### ティックレートの設定

NetworkManagerで設定:
```csharp
public class GameNetworkManager : NetworkManager
{
    void Start()
    {
        // 30Hzで初期化
        Initialize(tickRate: 30);
    }
}
```

#### TickManagerと予測システムの関係

PredictedTransformはティックベースで動作:
```
サーバーティック: 1000
クライアントティック: 1003 (100msのping = 3ティック先行)

クライアント予測:
- ティック1000: サーバー状態受信
- ティック1001: 予測実行
- ティック1002: 予測実行
- ティック1003: 予測実行 (現在)

サーバーから1000ティックの正しい状態が届く:
- ティック1000から再計算 (Reconcile)
- ティック1001を再シミュレーション
- ティック1002を再シミュレーション
- ティック1003に戻る
```

#### パフォーマンス最適化

**ティックレート vs フレームレート:**
```csharp
void Update()
{
    // 毎フレーム呼ばれる (60fps, 120fpsなど)
    // ビジュアル更新のみ行う
    UpdateGraphics();
}

protected override void NetworkFixedUpdate()
{
    // ティック毎に呼ばれる (30Hz固定など)
    // ゲームロジックを実行
    SimulateGameLogic();
}
```

- **Update**: 高フレームレートで滑らかな表示
- **NetworkFixedUpdate**: 固定ティックレートで決定的シミュレーション

**推奨設定:**
- ゲームロジック: 30Hz NetworkFixedUpdate
- ビジュアル: 60Hz+ Update
- 補間: TransformInterpolationで滑らかに

---

## 最適化システム

### NetworkVisibility - 関心範囲管理

#### 概要
NetworkVisibilityは、どのプレイヤーがどのオブジェクトを「見る」べきかを制御するシステム。遠くのオブジェクトやプレイヤーには情報を送信しないことで、帯域幅とCPU負荷を大幅に削減。

#### 動作原理

**Observer（観察者）の概念:**
```
プレイヤーA ─── 近い ───> オブジェクト1  [Observable]
プレイヤーB ─── 遠い ───X オブジェクト1  [Not Observable]
```

- プレイヤーAには オブジェクト1 の更新が送信される
- プレイヤーBには オブジェクト1 の情報は送信されない（帯域幅節約）

#### VisibilityRule の種類

##### 1. AlwaysVisibleRule - 常に可視
すべてのプレイヤーに常に表示。

```csharp
[CreateAssetMenu(menuName = "PurrNet/NetworkVisibility/Always Visible")]
public class AlwaysVisibleRule : NetworkVisibilityRule
{
    public override bool CanSee(PlayerID player, NetworkIdentity networkIdentity)
    {
        return true;  // 常にtrue
    }
}
```

**使用例:**
- グローバルUIオブジェクト
- ゲームマネージャー
- 全プレイヤーが知るべき重要オブジェクト

##### 2. DistanceRule - 距離ベース
プレイヤーからの距離に基づいて可視性を判定。

```csharp
[CreateAssetMenu(menuName = "PurrNet/NetworkVisibility/DistanceRule")]
public class DistanceRule : NetworkVisibilityRule
{
    [SerializeField] private LayerMask _layerMask = ~0;
    [SerializeField, Min(0)] private float _distance = 30f;      // 可視距離
    [SerializeField, Min(0)] private float _deadZone = 5f;       // ヒステリシス
    
    public override bool CanSee(PlayerID player, NetworkIdentity networkIdentity)
    {
        var myPos = networkIdentity.transform.position;
        bool wasPreviouslyVisible = networkIdentity.IsObserver(player);
        
        foreach (var playerIdentity in manager.EnumerateAllPlayerOwnedIds(player, true))
        {
            var playerPos = playerIdentity.transform.position;
            var distance = Vector3.Distance(myPos, playerPos);
            
            // ヒステリシス：以前見えていた場合は少し遠くまで見える
            float threshold = wasPreviouslyVisible ? _distance + _deadZone : _distance;
            
            if (distance <= threshold)
                return true;
        }
        
        return false;
    }
}
```

**パラメータ:**
- **distance**: 可視距離（例: 30メートル）
- **deadZone**: ヒステリシスゾーン（例: 5メートル）
  - オブジェクトが30m以内で見えるようになり、35m以上で見えなくなる
  - チラつき防止（30m境界付近での表示/非表示の高速切り替え防止）
- **layerMask**: どのレイヤーのプレイヤーオブジェクトを基準にするか

**使用例:**
- 環境オブジェクト
- NPC
- プレイヤーキャラクター
- アイテム

##### 3. NoVisibilityRule - 非可視
誰にも表示しない（実質的に無効化）。

```csharp
[CreateAssetMenu(menuName = "PurrNet/NetworkVisibility/No Visibility")]
public class NoVisibilityRule : NetworkVisibilityRule
{
    public override bool CanSee(PlayerID player, NetworkIdentity networkIdentity)
    {
        return false;  // 常にfalse
    }
}
```

**使用例:**
- デバッグ用
- 一時的に同期を無効化したいオブジェクト

#### カスタムVisibilityRuleの作成

```csharp
[CreateAssetMenu(menuName = "Custom/TeamVisibilityRule")]
public class TeamVisibilityRule : NetworkVisibilityRule
{
    public override bool CanSee(PlayerID player, NetworkIdentity networkIdentity)
    {
        // 同じチームのプレイヤーにのみ表示
        if (!networkIdentity.TryGetComponent<TeamMember>(out var targetTeam))
            return false;
            
        foreach (var playerIdentity in manager.EnumerateAllPlayerOwnedIds(player, true))
        {
            if (playerIdentity.TryGetComponent<TeamMember>(out var playerTeam))
            {
                if (playerTeam.teamId == targetTeam.teamId)
                    return true;
            }
        }
        
        return false;
    }
}
```

**カスタムルール例:**
- チーム可視性（味方にのみ表示）
- 建物内可視性（同じ建物内のプレイヤーにのみ）
- ステルス可視性（特定条件下でのみ表示）
- レベルベース可視性（高レベルプレイヤーのみ表示）

#### 設定方法

**グローバル設定:**
```csharp
// NetworkManager の Inspector から
[SerializeField] private NetworkVisibilityRuleSet _visibilityRules;
```

1. `Assets/Create/PurrNet/NetworkVisibilityRuleSet` を作成
2. ルールを追加（AlwaysVisibleRule, DistanceRule など）
3. NetworkManager の Visibility Rules に設定

**オブジェクト個別設定:**
```csharp
// NetworkIdentity の Inspector から
[SerializeField] private NetworkVisibilityRuleSet _visibilityOverride;
```

個別オブジェクトに異なるルールを適用可能。

#### 動的なルール管理

```csharp
public class VisibilityManager : MonoBehaviour
{
    private NetworkManager networkManager;
    
    void Start()
    {
        networkManager = NetworkManager.main;
    }
    
    // ルール追加
    public void AddCustomRule(INetworkVisibilityRule rule)
    {
        networkManager.AddVisibilityRule(networkManager, rule);
    }
    
    // ルール削除
    public void RemoveCustomRule(INetworkVisibilityRule rule)
    {
        networkManager.RemoveVisibilityRule(rule);
    }
}
```

#### パフォーマンス影響

**帯域幅削減例:**
```
シナリオ: 100プレイヤー、200オブジェクト
AlwaysVisible: 100 × 200 = 20,000 更新/ティック
DistanceRule (30m): 平均 10オブジェクト/プレイヤー = 1,000 更新/ティック

削減率: 95%！
```

**CPU負荷:**
- **complexity**: ルールの複雑度（数値が大きいほど重い）
- AlwaysVisibleRule: complexity = 0（最軽量）
- DistanceRule: complexity = 100（距離計算あり）
- カスタムルール: 処理内容に応じて設定

**最適化のベストプラクティス:**
1. 重要なオブジェクトのみ AlwaysVisible
2. 大半のオブジェクトは DistanceRule
3. 距離は必要最小限に（30-50mが一般的）
4. DeadZone を設定してチラつき防止
5. LayerMask で計算対象を絞る

#### 実用例

```csharp
// ゲームマネージャー設定例
public class GameSetup : MonoBehaviour
{
    [SerializeField] private NetworkVisibilityRuleSet globalRules;
    [SerializeField] private NetworkVisibilityRuleSet playerRules;
    [SerializeField] private NetworkVisibilityRuleSet itemRules;
    
    void SetupVisibility()
    {
        // グローバルオブジェクト: 常に可視
        // globalRules に AlwaysVisibleRule を設定
        
        // プレイヤー: 50m以内
        // playerRules に DistanceRule(distance=50, deadZone=10) を設定
        
        // アイテム: 30m以内
        // itemRules に DistanceRule(distance=30, deadZone=5) を設定
    }
}
```

---

### StatisticsManager - パフォーマンス監視

#### 概要
ネットワークパフォーマンスをリアルタイムで監視・表示するシステム。Ping、帯域幅使用量、パケットロスなどの統計情報を提供。

#### 基本設定

```csharp
public class StatisticsManager : MonoBehaviour
{
    [Range(0.05f, 1f)] public float checkInterval = 0.33f;  // 更新間隔
    [SerializeField] private StatisticsPlacement placement = StatisticsPlacement.TopLeft;
    [SerializeField] private StatisticsDisplayType _displayType = 
        StatisticsDisplayType.Ping | StatisticsDisplayType.Usage;
    [SerializeField] private StatisticsDisplayTarget _displayTarget = 
        StatisticsDisplayTarget.Editor | StatisticsDisplayTarget.Build;
}
```

#### 取得可能な統計情報

##### 1. Ping（遅延）
```csharp
public int ping { get; private set; }  // ミリ秒単位

// 使用例
void Update()
{
    var stats = FindObjectOfType<StatisticsManager>();
    int currentPing = stats.ping;
    
    if (currentPing > 200)
    {
        ShowHighLatencyWarning();
    }
}
```

**Ping の目安:**
- 0-30ms: 優秀（ローカルネットワーク）
- 30-60ms: 良好（同地域）
- 60-100ms: 普通（異なる地域）
- 100-200ms: 微妙（遠距離）
- 200ms以上: 悪い（ラグを感じる）

##### 2. Jitter（遅延揺らぎ）
```csharp
public int jitter { get; private set; }  // ミリ秒単位

// Jitterが高い = 不安定な接続
```

**Jitter の目安:**
- 0-10ms: 安定
- 10-30ms: やや不安定
- 30ms以上: 非常に不安定

##### 3. Packet Loss（パケットロス）
```csharp
public int packetLoss { get; private set; }  // パーセント

// パケットロスが高い = データが失われている
```

**Packet Loss の目安:**
- 0-1%: 問題なし
- 1-5%: 軽微な影響
- 5-10%: 顕著な影響
- 10%以上: 深刻な問題

##### 4. Upload/Download（帯域幅使用量）
```csharp
public float upload { get; private set; }    // KB/s
public float download { get; private set; }  // KB/s

// 使用例
void CheckBandwidth()
{
    var stats = FindObjectOfType<StatisticsManager>();
    float totalBandwidth = stats.upload + stats.download;
    
    Debug.Log($"合計帯域幅: {totalBandwidth:F2} KB/s");
    
    if (totalBandwidth > 500)  // 500KB/s = 4Mbps
    {
        Debug.LogWarning("高帯域幅使用中！");
    }
}
```

**帯域幅の目安:**
- 10-50 KB/s: 低帯域幅ゲーム
- 50-200 KB/s: 通常のゲーム
- 200-500 KB/s: 高頻度更新ゲーム
- 500KB/s以上: 最適化推奨

#### 表示設定

**StatisticsDisplayType（表示内容）:**
```csharp
[Flags]
public enum StatisticsDisplayType
{
    Ping = 1,           // Ping表示
    Usage = 2,          // 帯域幅表示
    PacketLoss = 4,     // パケットロス表示
    Jitter = 8          // Jitter表示
}

// 組み合わせ使用
_displayType = StatisticsDisplayType.Ping | StatisticsDisplayType.Usage;
```

**StatisticsPlacement（表示位置）:**
```csharp
public enum StatisticsPlacement
{
    None,           // 非表示
    TopLeft,        // 左上
    TopRight,       // 右上
    BottomLeft,     // 左下
    BottomRight     // 右下
}
```

**StatisticsDisplayTarget（表示対象）:**
```csharp
[Flags]
public enum StatisticsDisplayTarget
{
    Editor = 1,     // エディタ内で表示
    Build = 2       // ビルド版で表示
}

// エディタのみ表示
_displayTarget = StatisticsDisplayTarget.Editor;

// 常に表示
_displayTarget = StatisticsDisplayTarget.Editor | StatisticsDisplayTarget.Build;
```

#### 実用例

**カスタムネットワーク品質表示:**
```csharp
public class NetworkQualityUI : MonoBehaviour
{
    [SerializeField] private StatisticsManager statisticsManager;
    [SerializeField] private Image qualityIndicator;
    [SerializeField] private Text pingText;
    
    void Update()
    {
        UpdateQualityIndicator();
    }
    
    void UpdateQualityIndicator()
    {
        int ping = statisticsManager.ping;
        int jitter = statisticsManager.jitter;
        int packetLoss = statisticsManager.packetLoss;
        
        // 接続品質の評価
        Color indicatorColor;
        string quality;
        
        if (ping < 60 && jitter < 10 && packetLoss < 1)
        {
            indicatorColor = Color.green;
            quality = "優秀";
        }
        else if (ping < 100 && jitter < 30 && packetLoss < 5)
        {
            indicatorColor = Color.yellow;
            quality = "普通";
        }
        else
        {
            indicatorColor = Color.red;
            quality = "悪い";
        }
        
        qualityIndicator.color = indicatorColor;
        pingText.text = $"Ping: {ping}ms ({quality})";
    }
}
```

**帯域幅モニタリング:**
```csharp
public class BandwidthMonitor : MonoBehaviour
{
    [SerializeField] private StatisticsManager statisticsManager;
    private float peakUpload;
    private float peakDownload;
    
    void Update()
    {
        peakUpload = Mathf.Max(peakUpload, statisticsManager.upload);
        peakDownload = Mathf.Max(peakDownload, statisticsManager.download);
    }
    
    public void LogBandwidthStats()
    {
        Debug.Log($"現在の Upload: {statisticsManager.upload:F2} KB/s");
        Debug.Log($"現在の Download: {statisticsManager.download:F2} KB/s");
        Debug.Log($"ピーク Upload: {peakUpload:F2} KB/s");
        Debug.Log($"ピーク Download: {peakDownload:F2} KB/s");
        Debug.Log($"合計ピーク: {(peakUpload + peakDownload):F2} KB/s");
    }
}
```

**動的品質調整:**
```csharp
public class AdaptiveQuality : MonoBehaviour
{
    [SerializeField] private StatisticsManager statisticsManager;
    private PredictedTransform[] allPredictedTransforms;
    
    void Start()
    {
        allPredictedTransforms = FindObjectsOfType<PredictedTransform>();
        InvokeRepeating(nameof(AdjustQuality), 1f, 5f);  // 5秒毎に調整
    }
    
    void AdjustQuality()
    {
        float totalBandwidth = statisticsManager.upload + statisticsManager.download;
        int ping = statisticsManager.ping;
        
        // 高帯域幅または高Pingの場合、精度を下げる
        FloatAccuracy targetAccuracy;
        
        if (totalBandwidth > 400 || ping > 150)
        {
            targetAccuracy = FloatAccuracy.Low;
            Debug.Log("ネットワーク品質低下 → 精度をLowに");
        }
        else if (totalBandwidth > 200 || ping > 100)
        {
            targetAccuracy = FloatAccuracy.Medium;
            Debug.Log("ネットワーク品質普通 → 精度をMediumに");
        }
        else
        {
            targetAccuracy = FloatAccuracy.Purrfect;
            Debug.Log("ネットワーク品質良好 → 精度をPurrfectに");
        }
        
        // 全PredictedTransformの精度を調整
        foreach (var pt in allPredictedTransforms)
        {
            // pt.floatAccuracy = targetAccuracy; // プロパティがあれば
        }
    }
}
```

#### デバッグ活用

**接続問題の診断:**
```csharp
public class NetworkDiagnostics : MonoBehaviour
{
    [SerializeField] private StatisticsManager statisticsManager;
    
    public string DiagnoseConnection()
    {
        int ping = statisticsManager.ping;
        int jitter = statisticsManager.jitter;
        int packetLoss = statisticsManager.packetLoss;
        
        if (packetLoss > 10)
            return "深刻なパケットロス - ネットワーク接続を確認";
            
        if (jitter > 50)
            return "非常に不安定な接続 - Wi-Fiの場合は有線接続を試す";
            
        if (ping > 200)
            return "高遅延 - サーバーが遠すぎる可能性";
            
        if (ping > 100)
            return "中程度の遅延 - プレイ可能だがラグを感じる可能性";
            
        return "接続良好";
    }
}
```

---

## 総合活用ガイド

### プレイヤーキャラクターの推奨構成

```csharp
[PredictedTransform設定]
FloatAccuracy: Medium
UnparentGraphics: false
CharacterControllerPatch: true (CharacterController使用時)
InterpolationSettings: 有効
  
[ColliderRollback]
StoreHistoryInSeconds: 5.0
AutoAddAllChildren: true

[通信設定]
- 移動入力: ServerRpc, Channel.UnreliableSequenced
- 攻撃: ServerRpc, Channel.ReliableOrdered
- ダメージ通知: TargetRpc, Channel.ReliableOrdered
- エフェクト: ObserversRpc, Channel.ReliableUnordered
```

### 武器・戦闘システムの推奨構成

```csharp
// クライアント側
public class WeaponController : NetworkBehaviour
{
    // 入力送信: 高頻度、順序あり
    [ServerRpc(channel: Channel.UnreliableSequenced)]
    private void SendAimInput_ServerRpc(Vector3 aimDirection)
    {
        currentAimDirection = aimDirection;
    }
    
    // 射撃リクエスト: 重要、確実に届ける
    [ServerRpc(channel: Channel.ReliableOrdered)]
    private void RequestFire_ServerRpc(Vector3 fireDirection)
    {
        // サーバーで検証
        if (CanFire())
        {
            // ラグ補償付きヒット判定
            PerformLagCompensatedHitDetection(fireDirection);
        }
    }
    
    // エフェクト: 確実だが順序不要
    [ObserversRpc(channel: Channel.ReliableUnordered)]
    private void PlayFireEffect_ObserversRpc()
    {
        muzzleFlash.Play();
        audioSource.PlayOneShot(fireSound);
    }
}

// サーバー側ヒット判定
private void PerformLagCompensatedHitDetection(Vector3 direction)
{
    // クライアントの視点時間を取得
    var clientTick = GetClientTick();
    var rollback = NetworkManager.main.rollbackModule;
    
    // ラグ補償付きレイキャスト
    if (rollback.SphereCast(clientTick, firePoint.position, 0.1f, 
        direction, out var hit, maxRange, hitLayerMask))
    {
        // ヒット処理
        ProcessHit(hit);
    }
}
```

### 環境オブジェクトの推奨構成

```csharp
// 破壊可能オブジェクト
[NetworkTransform]
FloatAccuracy: Low (環境オブジェクトは低精度でOK)
SendIntervalInSeconds: 0.1 (頻繁な更新不要)

[ColliderRollback]
StoreHistoryInSeconds: 5.0 (ヒット判定対象)
AutoAddAllChildren: true

[SyncVar]
health: ServerAuth, SendInterval = 0 (即座に同期)
isDestroyed: ServerAuth, SendInterval = 0
```

### 帯域幅最適化戦略

#### レイヤー1: 精度削減
```csharp
// 重要なオブジェクト: Medium (0.001精度)
playerTransform.floatAccuracy = FloatAccuracy.Medium;

// 背景オブジェクト: Low (16bit half)
backgroundObject.floatAccuracy = FloatAccuracy.Low;
```

#### レイヤー2: 送信頻度削減
```csharp
// 重要でない値は送信間隔を開ける
temperatureSyncVar.sendIntervalInSeconds = 1.0f;

// Channel選択
[ServerRpc(channel: Channel.UnreliableSequenced)]  // 高頻度、低重要度
[ServerRpc(channel: Channel.ReliableOrdered)]      // 低頻度、高重要度
```

#### レイヤー3: 差分圧縮
```csharp
// DeltaModuleが自動的に差分のみ送信
// PredictedTransform/PredictedRigidbodyは自動対応
deltaModule.WriteReliable(packer, target, key, newState);
```

#### レイヤー4: 関心範囲管理
```csharp
// 遠いプレイヤーには低頻度で送信
// または完全に送信停止
// (PurrNetの関心範囲システムを使用)
```

### デバッグ・最適化ツール

#### 帯域幅プロファイリング
```csharp
// TickBandwidthProfiler - パケット統計
// PurrNet Profiler ウィンドウで確認可能
- 送受信バイト数
- RPC呼び出し回数
- ティック毎の内訳
```

#### ティック可視化
```csharp
void OnGUI()
{
    var tick = networkManager.tickModule.currentTick;
    var ping = connection.ping;
    GUI.Label(new Rect(10, 10, 200, 20), $"Tick: {tick}, Ping: {ping}ms");
}
```

#### Rollback可視化（カスタム実装推奨）
```csharp
void OnDrawGizmos()
{
    if (!Application.isPlaying) return;
    
    var rollback = NetworkManager.main.rollbackModule;
    var currentTick = networkManager.tickModule.currentTick;
    
    // 現在の位置
    Gizmos.color = Color.green;
    Gizmos.DrawWireSphere(transform.position, 0.5f);
    
    // 3秒前の位置
    var pastTick = currentTick - 90;  // 30Hz * 3秒
    if (rollback.TryGetColliderState(pastTick, GetComponent<Collider>(), out var state))
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(state.position, 0.5f);
        Gizmos.DrawLine(state.position, transform.position);
    }
}
```

### パフォーマンスチェックリスト

#### ネットワーク帯域幅
- [ ] FloatAccuracy を適切に設定（重要度に応じて Medium/Low）
- [ ] 頻繁に変わる非重要値は SyncVar の sendInterval を設定
- [ ] RPC の Channel を適切に選択（高頻度 = Unreliable系）
- [ ] DeltaModule を活用（PredictedTransform使用で自動）

#### CPU負荷
- [ ] ティックレートは 30Hz 程度（競技ゲーム以外）
- [ ] ColliderRollback の履歴期間を必要最小限に（5秒が妥当）
- [ ] Rollback のレイヤーマスクを適切に設定

#### メモリ使用量
- [ ] ColliderRollback の対象を必要なオブジェクトのみに
- [ ] 履歴保存期間を調整（_storeHistoryInSeconds）
- [ ] 使わない SyncVar イベントは登録解除

### トラブルシューティング

#### 「壁の裏で撃たれる」問題
**原因**: ラグ補償未実装  
**解決**: ColliderRollback使用、クライアントティックでの判定
```csharp
var clientTick = playerConnection.GetClientTick();
rollbackModule.SphereCast(clientTick, ...);
```

#### 「カクカクした動き」問題
**原因**: 予測エラーの瞬間補正  
**解決**: TransformInterpolationSettings設定
```csharp
useInterpolation = true
correctionRateMinMax = (3.3, 10.0)
```

#### 「入力遅延が大きい」問題
**原因**: クライアント予測未使用  
**解決**: PredictedTransform/PredictedRigidbody使用

#### 「帯域幅が大きすぎる」問題
**原因**: 高精度、高頻度送信  
**解決**: 
1. FloatAccuracy を Medium/Low に
2. Channel を Unreliable系に
3. SyncVar の sendInterval 設定

---

## 設定早見表

### 現在のプロジェクト設定

| コンポーネント | 設定項目 | PlayerController | PlayerCore |
|--------------|---------|-----------------|------------|
| PredictedTransform | FloatAccuracy | Medium | Medium |
| | UnparentGraphics | false | false |
| | CharacterControllerPatch | false | true |
| | InterpolationSettings | ? | ? |
| ColliderRollback | Attached | ? | ? |
| | StoreHistoryInSeconds | - | - |

### 推奨設定サマリー

**プレイヤーキャラクター:**
```
PredictedTransform: FloatAccuracy=Medium, Interpolation=有効
ColliderRollback: 5秒履歴
RPC: 入力=UnreliableSequenced, アクション=ReliableOrdered
```

**武器:**
```
サーバー判定: RollbackModule.SphereCast(clientTick)
RPC: TargetRpc=ReliableOrdered, ObserversRpc=ReliableUnordered
```

**環境:**
```
NetworkTransform: FloatAccuracy=Low
ColliderRollback: 必要な場合のみ
SyncVar: SendInterval設定
```

**ネットワーク:**
```
TickRate: 30Hz
Transport: UDP/Steam
Channel: 用途に応じて選択
```

---

## まとめ

PurrNetは以下の包括的な通信機能を提供:

### クライアント予測＆同期
- **PredictedTransform/Rigidbody**: ゼロ遅延の入力応答
- **FloatAccuracy**: 3段階の精度設定でデータサイズ最適化
- **DeltaModule**: 差分圧縮で帯域幅30-90%削減

### ラグ補償
- **ColliderRollback**: サーバー側で過去時点の当たり判定
- **TransformInterpolation**: エラー補正を滑らかに

### 通信プリミティブ
- **RPC**: ServerRpc/ObserversRpc/TargetRpc で柔軟な通信
- **SyncVar**: 自動同期変数で簡単な状態管理
- **Channel**: 4種類の配信方法で用途に応じた最適化

### インフラ
- **TickManager**: 決定的シミュレーションのための固定ティック
- **NetworkTransform**: 非予測オブジェクト用の標準同期

これらを組み合わせることで、低遅延・高精度・低帯域幅のネットワークゲームを実現できます。

---

**作成日**: 2026年2月6日  
**対象パッケージ**: 
- dev.purrnet.purrnet@d2fbc960bd24 (Core)
- dev.purrnet.purrdiction@d36f6309e6df (Prediction)
