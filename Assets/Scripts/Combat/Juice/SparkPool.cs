using UnityEngine;

/// <summary>
/// Pooled directional spark particles (charter 6.3 + the locked charter-14 performance
/// budget: particles must never be Instantiate()'d per-hit). Pre-instantiates poolSize
/// inactive-emission (but active GameObject) ParticleSystem children once in Awake(); Play()
/// round-robins through the pool, repositioning/reorienting an existing instance rather than
/// ever calling Instantiate() again after Awake().
/// </summary>
public sealed class SparkPool : MonoBehaviour
{
    [SerializeField] private ParticleSystem sparkPrefab;
    [SerializeField] private int poolSize = 24;

    private ParticleSystem[] _pool;
    private int _nextIndex;

    private void Awake()
    {
        _pool = new ParticleSystem[poolSize];

        if (sparkPrefab == null)
            return;

        for (int i = 0; i < poolSize; i++)
        {
            ParticleSystem instance = Instantiate(sparkPrefab, transform);
            instance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _pool[i] = instance;
        }
    }

    /// <summary>
    /// Plays the next pooled spark instance at the given contact point, oriented along the
    /// given surface normal. Round-robins through the pool (no availability check needed —
    /// bursts are short-lived relative to the 24-instance pool size at expected hit rates).
    /// No-op if the pool wasn't populated (no prefab wired).
    /// </summary>
    public void Play(Vector3 point, Vector3 normal)
    {
        if (_pool == null || _pool.Length == 0)
            return;

        ParticleSystem instance = _pool[_nextIndex];
        _nextIndex = (_nextIndex + 1) % _pool.Length;

        if (instance == null)
            return;

        instance.transform.position = point;
        instance.transform.rotation = normal.sqrMagnitude > 0f
            ? Quaternion.LookRotation(normal)
            : Quaternion.identity;

        instance.Clear();
        instance.Play();
    }
}
