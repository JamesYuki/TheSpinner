# テレポートシステム セットアップガイド

## 概要
パックを1P/2P間でテレポートさせるシステムです。同じ色のテレポートゾーンがペアとなり、時間経過でランダムにシャッフルされます。

### アーキテクチャ
- **TeleportManager** — 純粋な`MonoBehaviour`ユーティリティサービス（`NetworkBehaviour`ではない）
  - ゾーン収集・ペアマッピング・テレポート実行・シャッフル適用
- **RoundRunningState** — `PredictedStateNode`内でシャッフルタイマーと`PredictedRandom`を管理
  - シャッフルの状態が予測ステートの一部としてロールバック・リプレイに対応

---

## 1. TeleportManager のセットアップ

### 1.1 TeleportManager オブジェクトの作成
1. シーンに空の GameObject を作成（例: "TeleportManager"）
2. `TeleportManager` コンポーネントをアタッチ
3. **注意**: `NetworkIdentity` は不要です（純粋なMonoBehaviourのため）

### 1.2 スロット位置の設定
テレポートゾーンがシャッフルで移動する「配置位置」を定義します。

#### 左エリア（1P側）のスロット:
1. TeleportManager の子オブジェクトとして空の Transform を3つ作成:
   - `LeftSlot_0` (位置: -10, 0, 5)
   - `LeftSlot_1` (位置: -10, 0, 0)
   - `LeftSlot_2` (位置: -10, 0, -5)
2. これらを TeleportManager の Inspector の `Left Slots` リストに追加

#### 右エリア（2P側）のスロット:
1. 同様に右側用の Transform を3つ作成:
   - `RightSlot_0` (位置: 10, 0, 5)
   - `RightSlot_1` (位置: 10, 0, 0)
   - `RightSlot_2` (位置: 10, 0, -5)
2. これらを `Right Slots` リストに追加

### 1.3 シャッフル設定
シャッフルの設定は **RoundRunningState** コンポーネントの Inspector で行います：
- `Shuffle Interval Seconds`: シャッフル間隔（デフォルト: 15秒）
- `Initial Shuffle Delay`: 最初のシャッフルまでの遅延（デフォルト: 10秒）
- `Shuffle Enabled`: チェックを入れて有効化

> **注意**: シャッフルは PredictedStateNode の StateSimulate 内で実行されるため、
> 予測・ロールバックに完全対応しています。

---

## 2. TeleportZone のセットアップ

### 2.1 基本オブジェクトの作成
1. GameObject を作成（例: "TeleportZone_Red_Left"）
2. **必須コンポーネント**を追加:
   - `TeleportZone` スクリプト
   - **Collider**（Box/Sphere/Capsule）→ `Is Trigger` にチェック
   - **Rigidbody** → `Is Kinematic` にチェック、`Use Gravity` のチェックを外す

> **⚠️ 重要**: Rigidbody を Kinematic に設定しないと、PurrNet の予測システムでトリガーイベントが動作しません！

### 2.2 TeleportZone の設定
- `Color Id`: Red/Blue/Yellow/Green/Purple から選択（ペアで同じ色を使用）
- `Team Side`: 0 = 1P左側, 1 = 2P右側

### 2.3 出口位置の設定
1. TeleportZone の子オブジェクトとして空の Transform を作成（例: "ExitPoint"）
2. この Transform を少し前方にオフセット（例: ローカル Z = 2）
3. TeleportZone の Inspector で `Exit Point` に設定

```
TeleportZone_Red_Left
├─ ExitPoint (Transform)       ← ここにパックが出現
└─ ExitDirection (Transform)   ← (オプション) 射出方向
```

### 2.4 ビジュアル設定（オプション）
- `Renderer`: ゾーンのメッシュレンダラー（色が自動適用されます）
- `Teleport Effect`: パーティクルシステム（テレポート時に再生）

---

## 3. ペア構成の例

### 例: 3色のペア
左エリア（TeamSide=0）:
- TeleportZone_Red_Left (ColorId=Red)
- TeleportZone_Blue_Left (ColorId=Blue)
- TeleportZone_Yellow_Left (ColorId=Yellow)

右エリア（TeamSide=1）:
- TeleportZone_Red_Right (ColorId=Red)    ← Red_Left とペア
- TeleportZone_Blue_Right (ColorId=Blue)  ← Blue_Left とペア
- TeleportZone_Yellow_Right (ColorId=Yellow) ← Yellow_Left とペア

> **ルール**: 同じ ColorId が左右に1つずつ必要。複数あるとワーニングが出ます。

---

## 4. Puck（パック）の設定確認

### 4.1 必須コンポーネント
- `Puck` スクリプト
- `PredictedRigidbody` コンポーネント
- Collider（Sphere 推奨）

### 4.2 PredictedRigidbody の設定
Inspector で以下を確認:
- `Event Mask` → **TriggerEnter にチェックが入っていること**（デフォルトで有効）
- `Rigidbody` → `Is Kinematic` のチェックを外す（動的オブジェクト）

---

## 5. トラブルシューティング

### ❌ テレポートが動作しない

#### チェック1: コンソールにログが出ているか
実行時にコンソールを確認:
```
[Puck] トリガー検知: TeleportZone_Red_Left
[Puck] TeleportZone検出: Red, Side=0
[Puck] テレポート成功: Red
[TeleportManager] テレポートペア: Red (左 ↔ 右)
```
→ これらのログが出ない場合、トリガーが検知されていません。

#### チェック2: TeleportZone の Rigidbody 設定
- **Rigidbody コンポーネントが追加されているか**
- **Is Kinematic = true になっているか**
- **Collider の Is Trigger = true になっているか**

→ `OnValidate` で自動設定されますが、手動確認してください。

#### チェック3: Puck の PredictedRigidbody 設定
Inspector で `PredictedRigidbody` を開く:
- `Event Mask` に `TriggerEnter` が含まれているか確認
  - デフォルトは `0x3F` (全イベント有効) なので通常問題なし
  - もし `None` になっていたら、手動で `TriggerEnter` を有効化

#### チェック4: TeleportManager が配置されているか
```
[TeleportManager] 左ゾーン: 3, 右ゾーン: 3
[TeleportManager] ペア構築: Red (左 ↔ 右)
```
→ ゲーム開始時にこのログが出ない場合、TeleportManager が正しく配置されていません。

**解決策**:
- TeleportManager がシーンに配置されているか確認
- TeleportManager は純粋なMonoBehaviourなので NetworkIdentity は不要です

#### チェック5: 物理レイヤー設定
Project Settings > Physics で以下を確認:
- Puck のレイヤーと TeleportZone のレイヤーが衝突可能か
  - レイヤーマトリックスでチェックが入っているか確認

---

### ❌ シャッフルが動作しない

#### チェック1: 予測ループ内で実行されているか
シャッフルは RoundRunningState.StateSimulate 内で実行されます。
- RoundRunningState が正しくステートマシンに登録されているか確認
- ラウンドが開始しているか（Enter が呼ばれているか）確認

#### チェック2: Shuffle Enabled がオンか
RoundRunningState の Inspector で `Shuffle Enabled` にチェックが入っているか確認。

#### チェック3: スロット数とゾーン数
- Left Slots と Left ゾーンの数が一致しているか
- Right Slots と Right ゾーンの数が一致しているか

不一致の場合、余ったゾーンはシャッフルされません。

---

### ❌ ペアが見つからない

コンソールに以下のワーニングが出ます:
```
[TeleportManager] 色 Red のペアが見つかりません（右エリアにありません）
```

**原因**:
- 左エリアに Red があるが、右エリアに Red がない
- または ColorId の設定ミス

**解決策**:
- 左右に同じ ColorId のゾーンを配置
- Scene ウィンドウで Gizmo を確認（色とラベルが表示されます）

---

## 6. デバッグ機能

### Gizmo 表示
Scene ビューで以下が表示されます:
- **TeleportZone**: ワイヤー球 + 色ラベル + 出口位置（シアン）
- **TeleportManager スロット**: ワイヤー立方体 + L0, L1, R0, R1... のラベル

### ログ出力
`AppLogger` を使用しているため、`AppLoggerSettings.EnableLog` で制御できます。

デバッグ時は以下を有効化:
```csharp
AppLoggerSettings.EnableLog = true;
AppLoggerSettings.EnableEditorLog = true;
```

---

## 7. 動作フロー

1. **初期化** (RoundRunningState.Enter):
   - TeleportManager.Initialize() でシーン内の全 TeleportZone を収集
   - 同じ ColorId のペアを構築
   - PredictedRandom で初期シャッフルを適用

2. **トリガー検知** (Puck.OnTriggerStart):
   - パックが TeleportZone のトリガーに入る
   - クールダウン中でなければテレポート実行

3. **テレポート実行** (TeleportManager.TryTeleport):
   - ペアのゾーンを取得
   - パックの位置をペアの ExitPosition に設定
   - パックの速度を ExitForward に沿って設定

4. **シャッフル** (RoundRunningState.StateSimulate 予測ループ内):
   - タイマーが 0 になると PredictedRandom から新しいシードを生成
   - TeleportManager.ApplyShuffleFromSeed() で決定的シャッフルを実行
   - ロールバック時は SetUnityState() でゾーン状態を復元

---

## 8. よくある質問

### Q: テレポートの瞬間にパックが止まる
**A**: 出口の速度が正しく設定されていません。
- ExitDirection Transform を設定して向きを調整
- または TeleportZone 自身の Forward 方向を調整

### Q: 無限テレポートループが発生する
**A**: クールダウンが短すぎます。
- Puck の Inspector で `Teleport Cooldown` を増やす（推奨: 0.5秒以上）

### Q: シャッフル後にペアが崩れる
**A**: 実装上、ペアは崩れません。
- ColorId は変わらず、位置だけが変わります
- もし崩れているなら、初期配置が間違っています

---

## 9. カスタマイズ

### シャッフルを手動で実行
RoundRunningState の Inspector からシャッフルタイマーをリセットするか、
エディタ上で TeleportManager の ContextMenu "Test Shuffle" を使用。

### シャッフル間隔を動的に変更
RoundRunningState の Inspector で `Shuffle Interval Seconds` を変更。
```

### 色を追加
`TeleportColorId.cs` に新しい色を追加:
```csharp
public enum TeleportColorId
{
    Red = 0,
    Blue = 1,
    Yellow = 2,
    Green = 3,
    Purple = 4,
    Orange = 5,  // 追加
}
```

`TeleportManager.cs` の `s_ColorMap` にも追加:
```csharp
{ TeleportColorId.Orange, new Color(1f, 0.5f, 0f) },
```

---

## まとめ

✅ **必須チェックリスト**:
- [ ] TeleportManager がシーンに配置されている
- [ ] Left Slots / Right Slots が設定されている
- [ ] 各 TeleportZone に Collider (Is Trigger) + Rigidbody (Is Kinematic) がある
- [ ] ColorId のペアが左右に1つずつある
- [ ] Puck に PredictedRigidbody があり、Event Mask が有効
- [ ] コンソールでログを確認

これで完全に動作するはずです！
