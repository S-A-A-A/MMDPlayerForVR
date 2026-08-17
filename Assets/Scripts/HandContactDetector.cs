using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// MMDモデルのボーンコライダーに付与するランタイムコンポーネント。
/// 指定レイヤー（HandCapsule）のColliderとの接触を検知し、
/// OnTouchStart / OnTouchEnd イベントを発火する。
///
/// 設計上の注意:
/// Meta XR SDKの Hand Physics Capsules は1本の指に複数のCapsuleColliderを生成する。
/// そのため単純な OnTriggerEnter/Exit をそのままイベント化すると、
/// 指が触れている間に Enter/Exit が繰り返し発火する。
/// HashSetで接触中Colliderを追跡し、0→1のときのみ OnTouchStart、
/// n→0のときのみ OnTouchEnd を発火することで多重発火を防ぐ。
/// </summary>
[RequireComponent(typeof(Collider))]
public sealed class HandContactDetector : MonoBehaviour
{
    [SerializeField]
    [Tooltip("接触判定の対象とするレイヤー（HandCapsuleレイヤーを指定）")]
    private LayerMask _handLayer;

    [SerializeField]
    [Tooltip("Consoleへのログ出力の有無（検証用。本番では無効化を推奨）")]
    private bool _logToConsole = true;

    // BoneColliderGeneratorが生成時に設定するボーン名。
    // 外部クラスが読み取れるよう getter のみ公開する。
    [SerializeField]
    [Tooltip("所属するボーン名（BoneColliderGeneratorが自動設定）")]
    private string _boneName;

    [SerializeField]
    [Tooltip("最初の接触開始時に発火。引数は接触した手側のTransform")]
    private UnityEvent<Transform> _onTouchStart;

    [SerializeField]
    [Tooltip("すべての接触が終了した時に発火。引数は最後に離れた手側のTransform")]
    private UnityEvent<Transform> _onTouchEnd;

    // 現在接触中のColliderを追跡する。
    // Listではなく HashSet を使う理由: OnTriggerEnter が同一Colliderに対して
    // 複数回呼ばれるエッジケースでも Add/Remove の等値判定で重複を排除できるため。
    private readonly HashSet<Collider> _touchingColliders = new HashSet<Collider>();

    /// <summary>BoneColliderGeneratorが読み取れるようにgetter公開。外部からの書き換えは禁止。</summary>
    public string BoneName => _boneName;

    /// <summary>現在何かに触れているかどうか。</summary>
    public bool IsTouching => _touchingColliders.Count > 0;

    /// <summary>現在接触中のCollider数（デバッグ・外部モニタリング用）。</summary>
    public int TouchingCount => _touchingColliders.Count;

    /// <summary>
    /// BoneColliderGeneratorがコライダー生成直後に呼び出して初期値を設定するメソッド。
    /// Inspectorを介さず設定できるようにすることで、Editorスクリプトとの依存を明示する。
    /// </summary>
    public void Initialize(string boneName, LayerMask handLayer)
    {
        _boneName = boneName;
        _handLayer = handLayer;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsInLayerMask(other.gameObject.layer, _handLayer))
        {
            return;
        }

        bool wasEmpty = _touchingColliders.Count == 0;
        _touchingColliders.Add(other);

        // カウンターが 0→1 になった（= 最初の接触）ときのみ発火する
        if (wasEmpty)
        {
            if (_logToConsole)
            {
                Debug.Log($"[MMD接触] {_boneName} に手が触れました ({other.name})");
            }

            _onTouchStart?.Invoke(other.transform);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsInLayerMask(other.gameObject.layer, _handLayer))
        {
            return;
        }

        _touchingColliders.Remove(other);

        // カウンターが n→0 になった（= 全接触終了）ときのみ発火する
        if (_touchingColliders.Count == 0)
        {
            if (_logToConsole)
            {
                Debug.Log($"[MMD接触] {_boneName} から手が離れました ({other.name})");
            }

            _onTouchEnd?.Invoke(other.transform);
        }
    }

    // Play Mode 終了や GameObject 非活性化のタイミングでセットをリセットする。
    // リセットしないと、次に Active になった際に古い接触状態が残留し
    // OnTouchStart が発火されないバグになるため。
    private void OnDisable()
    {
        _touchingColliders.Clear();
    }

    private static bool IsInLayerMask(int layerIndex, LayerMask mask)
    {
        return (mask.value & (1 << layerIndex)) != 0;
    }
}
