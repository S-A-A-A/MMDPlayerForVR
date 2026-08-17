using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// MMDモデルの選択ボーンを起点に、子ボーンとの間に CapsuleCollider を再帰的に自動生成する Editor ツール。
///
/// 使い方:
///   1. Hierarchy でボーンオブジェクトを1つ以上選択する
///   2. メニュー「MMD Tools > ボーンコライダー生成」を実行する
///   3. 不要になったら「MMD Tools > ボーンコライダー削除」で一括削除できる
///
/// 生成するコライダーは第1段階（接触検知・イベント発火）専用の検証実装。
/// Magica Cloth 2 導入後は本システムの役割を見直す必要がある。
/// 参照: 接触判定システム_詳細設計書_v0.1.md §1「目的・位置付け」
/// </summary>
public static class BoneColliderGenerator
{
    private const string MENU_GENERATE = "MMD Tools/ボーンコライダー生成";
    private const string MENU_REMOVE = "MMD Tools/ボーンコライダー削除";

    // 生成オブジェクトの識別に使うサフィックス。
    // このサフィックスを持つオブジェクトは二重生成防止・削除対象の判定に使う。
    private const string COLLIDER_SUFFIX = "Collider";

    // ボーン長に対するカプセル半径の比率。
    // 値の根拠: MMDモデルの標準的なボーン太さに対して視覚的に自然な判定範囲になるよう設計書で決定した。
    private const float RADIUS_RATIO = 0.2f;

    // 非常に短いボーン（指先など）でも最低限の接触判定ができるよう下限を設ける。
    private const float MIN_RADIUS = 0.003f;

    // ─────────────────────────────────────
    // メニュー: ボーンコライダー生成
    // ─────────────────────────────────────

    [MenuItem(MENU_GENERATE)]
    private static void GenerateForSelection()
    {
        Transform[] selected = Selection.transforms;
        if (selected == null || selected.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "ボーンコライダー生成",
                "Hierarchyでボーンを選択してから実行してください。",
                "OK");
            return;
        }

        Undo.SetCurrentGroupName("ボーンコライダー生成");
        int undoGroup = Undo.GetCurrentGroup();

        int totalCreated = 0;
        foreach (Transform bone in selected)
        {
            totalCreated += GenerateRecursive(bone);
        }

        Undo.CollapseUndoOperations(undoGroup);

        Debug.Log($"[BoneColliderGenerator] {totalCreated} 個のコライダーを生成しました。");
        EditorUtility.DisplayDialog(
            "ボーンコライダー生成",
            $"{totalCreated} 個のコライダーを生成しました。\n" +
            "生成した ~Collider の HandContactDetector コンポーネントで\n" +
            "Hand Layer を HandCapsule レイヤーに設定してください。",
            "OK");
    }

    [MenuItem(MENU_GENERATE, true)]
    private static bool ValidateGenerate()
    {
        return Selection.transforms.Length > 0;
    }

    // ─────────────────────────────────────
    // メニュー: ボーンコライダー削除
    // ─────────────────────────────────────

    [MenuItem(MENU_REMOVE)]
    private static void RemoveForSelection()
    {
        Transform[] selected = Selection.transforms;
        if (selected == null || selected.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "ボーンコライダー削除",
                "Hierarchyでボーン（またはコライダーの親ボーン）を選択してから実行してください。",
                "OK");
            return;
        }

        Undo.SetCurrentGroupName("ボーンコライダー削除");
        int undoGroup = Undo.GetCurrentGroup();

        int totalRemoved = 0;
        foreach (Transform bone in selected)
        {
            totalRemoved += RemoveRecursive(bone);
        }

        Undo.CollapseUndoOperations(undoGroup);

        Debug.Log($"[BoneColliderGenerator] {totalRemoved} 個のコライダーを削除しました。");
        EditorUtility.DisplayDialog(
            "ボーンコライダー削除",
            $"{totalRemoved} 個のコライダーを削除しました。",
            "OK");
    }

    [MenuItem(MENU_REMOVE, true)]
    private static bool ValidateRemove()
    {
        return Selection.transforms.Length > 0;
    }

    // ─────────────────────────────────────
    // 内部ロジック
    // ─────────────────────────────────────

    /// <summary>
    /// 指定ボーン (from) の直接子ボーン (to) との間にコライダーを生成し、再帰的に処理する。
    /// </summary>
    /// <returns>生成したコライダー数</returns>
    private static int GenerateRecursive(Transform from)
    {
        int count = 0;

        foreach (Transform child in from)
        {
            // COLLIDER_SUFFIX を持つオブジェクトは本ツールが生成したコライダーなのでスキップする。
            // スキップしないと再実行時に二重生成が発生する。
            if (child.name.EndsWith(COLLIDER_SUFFIX))
            {
                continue;
            }

            AddCapsuleBetween(from, child);
            count++;

            count += GenerateRecursive(child);
        }

        return count;
    }

    /// <summary>
    /// from → to の間に CapsuleCollider オブジェクトを生成し、
    /// Kinematic Rigidbody と HandContactDetector を付与する。
    /// </summary>
    private static void AddCapsuleBetween(Transform from, Transform to)
    {
        string objName = $"{from.name}_{COLLIDER_SUFFIX}";
        var go = new GameObject(objName);
        Undo.RegisterCreatedObjectUndo(go, $"生成: {objName}");

        // from の子として配置する。
        // ワールド座標を維持する（第3引数 worldPositionStays = true）ことで
        // 後続の position / rotation 設定が直感的に機能するようにする。
        Undo.SetTransformParent(go.transform, from, true, $"親設定: {objName}");

        // コライダーをワールド空間での from→to の中点に配置する。
        // CapsuleCollider は center から height/2 の距離を両端として持つため、
        // 中点に配置することで from と to の位置をちょうどカバーする。
        Vector3 worldMidpoint = (from.position + to.position) * 0.5f;
        go.transform.position = worldMidpoint;

        // ローカル Y 軸が from→to 方向を向くよう回転させる。
        // CapsuleCollider.direction = 1（Y 軸）と組み合わせることで
        // ボーン方向に沿ったカプセルが生成される。
        Vector3 direction = (to.position - from.position).normalized;
        if (direction != Vector3.zero)
        {
            go.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction);
        }

        float boneLength = Vector3.Distance(from.position, to.position);
        float radius = Mathf.Max(boneLength * RADIUS_RATIO, MIN_RADIUS);

        var capsule = Undo.AddComponent<CapsuleCollider>(go);
        capsule.direction = 1; // Y 軸方向
        capsule.height = boneLength;
        capsule.radius = radius;
        capsule.center = Vector3.zero;
        capsule.isTrigger = true;

        // isTrigger = true のコライダーが Physics イベントを受け取るには
        // Rigidbody が必要。モデルのボーンは物理シミュレーション対象外なので
        // isKinematic = true にしてアニメーションとの競合を防ぐ。
        var rb = Undo.AddComponent<Rigidbody>(go);
        rb.isKinematic = true;
        rb.useGravity = false;

        var detector = Undo.AddComponent<HandContactDetector>(go);
        // HandLayer は生成時点では設定しない。
        // LayerMask のデフォルト値は Nothing（0）なので、ユーザーが
        // Inspector 上で HandCapsule レイヤーを明示的に設定する必要がある。
        // ボーン名のみここで初期化する。
        detector.Initialize(from.name, default);
    }

    /// <summary>
    /// 指定ボーン以下のヒエラルキーから COLLIDER_SUFFIX を持つオブジェクトを再帰的に削除する。
    /// </summary>
    /// <returns>削除したオブジェクト数</returns>
    private static int RemoveRecursive(Transform root)
    {
        int count = 0;

        // 削除中にコレクションが変化するため、先にリスト化してから走査する。
        var children = new List<Transform>();
        foreach (Transform child in root)
        {
            children.Add(child);
        }

        foreach (Transform child in children)
        {
            if (child.name.EndsWith(COLLIDER_SUFFIX))
            {
                Undo.DestroyObjectImmediate(child.gameObject);
                count++;
            }
            else
            {
                count += RemoveRecursive(child);
            }
        }

        return count;
    }
}
