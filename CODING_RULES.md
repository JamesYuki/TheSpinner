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
