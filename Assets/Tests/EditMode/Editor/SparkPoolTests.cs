using NUnit.Framework;
using UnityEngine;

// Verifies the charter-14 locked performance-budget rule: SparkPool must never call
// Instantiate() outside Awake(). Proven here by counting pooled child GameObjects before and
// after several Play() calls and confirming the count never grows past poolSize.
public class SparkPoolTests
{
    private GameObject _go;
    private SparkPool _pool;
    private GameObject _prefabGo;
    private ParticleSystem _prefab;

    private const int PoolSize = 4;

    [SetUp]
    public void SetUp()
    {
        _prefabGo = new GameObject("SparkPrefabSource");
        _prefab = _prefabGo.AddComponent<ParticleSystem>();

        _go = new GameObject("SparkPoolHost");
        _pool = _go.AddComponent<SparkPool>();

        TestReflectionUtil.SetField(_pool, "sparkPrefab", _prefab);
        TestReflectionUtil.SetField(_pool, "poolSize", PoolSize);

        TestReflectionUtil.InvokeMethod(_pool, "Awake");
    }

    [TearDown]
    public void TearDown()
    {
        if (_go != null) Object.DestroyImmediate(_go);
        if (_prefabGo != null) Object.DestroyImmediate(_prefabGo);
    }

    [Test]
    public void Awake_PrePopulatesExactlyPoolSizeChildren()
    {
        Assert.AreEqual(PoolSize, _go.transform.childCount);
    }

    [Test]
    public void Play_RepeatedCalls_NeverGrowsChildCountBeyondPoolSize()
    {
        for (int i = 0; i < PoolSize * 3; i++)
        {
            _pool.Play(new Vector3(i, 0, 0), Vector3.up);
        }

        Assert.AreEqual(PoolSize, _go.transform.childCount, "Play() must never Instantiate() beyond the pool pre-populated in Awake().");
    }

    [Test]
    public void Play_PositionsAndOrientsTheRoundRobinInstance()
    {
        var point = new Vector3(1f, 2f, 3f);
        var normal = Vector3.right;

        _pool.Play(point, normal);

        ParticleSystem instance = _go.transform.GetChild(0).GetComponent<ParticleSystem>();
        Assert.AreEqual(point, instance.transform.position);
        AssertQuaternionApproximately(Quaternion.LookRotation(normal), instance.transform.rotation, 0.001f);
    }

    [Test]
    public void Play_RoundRobinsThroughEachPooledInstance()
    {
        for (int i = 0; i < PoolSize; i++)
        {
            _pool.Play(new Vector3(i, 0, 0), Vector3.up);
        }

        // Each of the PoolSize children should have been moved to its own distinct x position.
        for (int i = 0; i < PoolSize; i++)
        {
            ParticleSystem instance = _go.transform.GetChild(i).GetComponent<ParticleSystem>();
            Assert.AreEqual(i, instance.transform.position.x, 0.0001f);
        }
    }

    [Test]
    public void Play_WrapsAroundAfterExhaustingThePool()
    {
        for (int i = 0; i < PoolSize + 1; i++)
        {
            _pool.Play(new Vector3(i, 0, 0), Vector3.up);
        }

        // The (PoolSize+1)-th call wraps back to index 0, overwriting its position again.
        ParticleSystem firstInstance = _go.transform.GetChild(0).GetComponent<ParticleSystem>();
        Assert.AreEqual(PoolSize, firstInstance.transform.position.x, 0.0001f);
    }

    [Test]
    public void Awake_NoSparkPrefabWired_PopulatesNoChildren()
    {
        var bareGo = new GameObject("BarePoolHost");
        var barePool = bareGo.AddComponent<SparkPool>();
        TestReflectionUtil.SetField(barePool, "sparkPrefab", null);
        TestReflectionUtil.SetField(barePool, "poolSize", PoolSize);

        TestReflectionUtil.InvokeMethod(barePool, "Awake");

        Assert.AreEqual(0, bareGo.transform.childCount);

        Object.DestroyImmediate(bareGo);
    }

    [Test]
    public void Play_WithNoPrefabWired_DoesNotThrow()
    {
        var bareGo = new GameObject("BarePoolHost2");
        var barePool = bareGo.AddComponent<SparkPool>();
        TestReflectionUtil.SetField(barePool, "sparkPrefab", null);
        TestReflectionUtil.SetField(barePool, "poolSize", PoolSize);
        TestReflectionUtil.InvokeMethod(barePool, "Awake");

        Assert.DoesNotThrow(() => barePool.Play(Vector3.zero, Vector3.up));

        Object.DestroyImmediate(bareGo);
    }

    private static void AssertQuaternionApproximately(Quaternion expected, Quaternion actual, float tolerance)
    {
        float dot = Mathf.Abs(Quaternion.Dot(expected, actual));
        Assert.GreaterOrEqual(dot, 1f - tolerance);
    }
}
