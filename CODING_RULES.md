# コーディングルール

## AI アシスタント向けルール

### 必須チェック
1. **コンパイルエラーを残さない** - コード変更後は必ず `get_errors` でエラーチェックを行い、エラーがある場合は修正してから完了する
2. Debug.Logは使わないでAppLogを使うように

### PurrNet 関連
**細かい仕様は  "PurrNet_Documents.md"を確認して**

#### hierarchy.Create の使い方
`hierarchy.Create` は `PredictedObjectID?` を返す。`GameObject` ではない。

```csharp
// ✅ 正しい書き方
var objectId = predictionManager.hierarchy.Create(prefab, pos, rot, player);
if (objectId.HasValue)
{
    var component = objectId.GetComponent<MyComponent>(predictionManager);
}

// ❌ 間違った書き方（コンパイルエラーになる）
var obj = predictionManager.hierarchy.Create(prefab, pos, rot, player);
var component = obj.GetComponent<MyComponent>(); // エラー！
```

#### DisposableList の扱い
- `Enter()` で作成したら `Exit()` で `Dispose()` する
- アクセス前に `isDisposed` をチェックする
- `State.Dispose()` 内で二重Disposeを防ぐ

```csharp
public override void Exit()
{
    if (!currentState.players.isDisposed)
    {
        var state = currentState;
        state.players.Dispose();
        currentState = state;
    }
}
```

### UseRefCounter による状態管理

#### 基本ルール
**bool型で状態を管理する代わりに `UseRefCounter` を使用する**
- 複数の機能が同じ状態を参照する場合の参照カウント管理
- `Use()` で参照を増やし、`Dispose()` で参照を減らす
- **必ずスコープ内で完結させる**（使い終わったら必ず `Dispose()` を呼ぶ）

#### 実装パターン

```csharp
// ✅ 正しい実装例（Puckクラスより）
private IDisposable m_HideVisualHandle;
private UseRefCounter m_TeleportVisualRefCounter = new();

private void Awake()
{
    // カウンタが1以上になった時と0になった時のコールバックを設定
    m_TeleportVisualRefCounter.OnUse += () =>
    {
        HideVisual();
    };

    m_TeleportVisualRefCounter.OnReleased += () =>
    {
        ShowVisual();
    };
}

private void SomeMethod()
{
    // 既存のhandleがあれば先に解放（安全性のため）
    m_HideVisualHandle?.Dispose();
    
    // Use()を呼び出して参照を増やす
    m_HideVisualHandle = m_TeleportVisualRefCounter.Use();
}

private void OnDisable()
{
    // オブジェクト無効化時に必ずクリーンアップ
    m_HideVisualHandle?.Dispose();
    m_HideVisualHandle = null;
}
```

#### 注意点
1. **handle の管理**
   - `Use()` の戻り値（`IDisposable`）を必ず保持する
   - 既存の handle を上書きする前に `Dispose()` を呼ぶ
   - 使い終わったら `null` を代入して明示的にクリア

2. **ライフサイクル管理**
   - `OnDisable()` または `OnDestroy()` で必ず `Dispose()` を呼ぶ
   - リソースリークを防ぐため、すべてのパスで確実に解放する

3. **bool の代わりに使うべきケース**
   - 複数箇所から同じ状態を操作する場合
   - 状態のオン/オフが入れ子になる可能性がある場合
   - リソースの確実な解放が必要な場合

```csharp
// ❌ 避けるべきパターン（bool による管理）
private bool m_IsHidden = false;

private void SomeMethod()
{
    m_IsHidden = true;
    HideVisual();
    // ... 処理中に例外が起きたら？
    m_IsHidden = false; // これが実行されない可能性
    ShowVisual();
}

// ✅ UseRefCounter を使うべき
private IDisposable m_HideHandle;
private UseRefCounter m_HideRefCounter = new();

private void SomeMethod()
{
    m_HideHandle = m_HideRefCounter.Use();
    // using パターンも推奨（スコープを抜けると自動的に Dispose）
    using var handle = m_HideRefCounter.Use();
    // スコープ内で処理
} // 自動的に Dispose される
```
