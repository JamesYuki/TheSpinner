# Spinner System Structure

## Prefab 構造

### PlayerCore.prefab (hierarchy.Create で生成)
```
PlayerCore (ルート)
├─ PlayerCore.cs (PredictedIdentity)
├─ Core/
│   ├─ Sphere Collider (ダメージ判定)
│   └─ CoreVisual/
│       ├─ CoreMesh
│       ├─ Core_Trail
│       └─ Core_Emission
└─ SpinnerParent/ (Spinnerの親Transform)
```

### Player_Spinner.prefab (hierarchy.Create で別途生成)
```
Spinner (ルート)
├─ SpinnerController.cs (PredictedIdentity)
├─ SpinnerInputHandler.cs
├─ PredictedRigidbody
└─ ArmPivot/
    ├─ LeftArm/
    │   ├─ ArmCollider
    │   └─ ArmMesh
    └─ RightArm/
        ├─ ArmCollider
        └─ ArmMesh
```

---

## PurrNet 生成について

### hierarchy.Create を使用
PlayerCoreとSpinnerは両方とも `hierarchy.Create` で生成します。
これにより、PurrNetの予測システムに正しく登録されます。

### 生成の流れ (PlayerSpawningState)
```csharp
// 1. PlayerCoreを生成
var playerCoreObj = predictionManager.hierarchy.Create(
    m_PlayerCorePrefab, 
    spawnPoint.position, 
    spawnPoint.rotation, 
    player
);

// 2. Spinnerを生成
var spinnerObj = predictionManager.hierarchy.Create(
    m_SpinnerPrefab,
    spawnPoint.position,
    spawnPoint.rotation,
    player
);

// 3. SpinnerをPlayerCoreの子に設定
spinnerObj.transform.SetParent(playerCore.SpinnerParent);
playerCore.SetSpinner(spinnerController);
```

---

## 役割分離

| コンポーネント | 役割 | PredictedIdentity |
|---------------|------|-------------------|
| PlayerCore | ダメージ処理、Spinner参照保持 | **必要** |
| SpinnerController | 回転制御、パック衝突処理 | **必要** |

---

## メリット

1. **予測システム対応**: 両方とも `hierarchy.Create` で生成されるため、予測・ロールバックが正しく動作
2. **動的スキン変更**: Spinnerプレハブを変更することでスキン変更可能
3. **明確な分離**: PlayerCore = ダメージ, SpinnerController = 回転攻撃
