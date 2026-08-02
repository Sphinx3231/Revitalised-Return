using NUnit.Framework;
using UnityEngine;

// Top-centre compass strip (charter Step 11). CompassStrip is a thin per-frame driver over
// the already-tested pure CompassProjection functions (see CompassProjectionTests) -- these
// tests exercise CompassStrip's own logic: the null-guard early-outs on Update(), and
// PlaceMarker's visibility-driven SetActive/anchoredPosition wiring, cross-checked against
// CompassProjection's own outputs so a regression in either file would fail this suite.
public class CompassStripTests
{
    private GameObject _go;
    private CompassStrip _compass;

    private GameObject _playerGo;
    private GameObject _stripGo;
    private RectTransform _stripRect;

    private GameObject _northGo, _eastGo, _southGo, _westGo;
    private RectTransform _northRect, _eastRect, _southRect, _westRect;

    private const float HalfFov = 60f;
    private const float StripWidth = 200f; // halfWidth = 100

    [SetUp]
    public void SetUp()
    {
        _go = new GameObject("CompassStrip");
        _compass = _go.AddComponent<CompassStrip>();

        _playerGo = new GameObject("Player");

        _stripGo = new GameObject("Strip");
        _stripRect = _stripGo.AddComponent<RectTransform>();
        _stripRect.sizeDelta = new Vector2(StripWidth, 40f);

        _northGo = new GameObject("North");
        _northRect = _northGo.AddComponent<RectTransform>();
        _eastGo = new GameObject("East");
        _eastRect = _eastGo.AddComponent<RectTransform>();
        _southGo = new GameObject("South");
        _southRect = _southGo.AddComponent<RectTransform>();
        _westGo = new GameObject("West");
        _westRect = _westGo.AddComponent<RectTransform>();

        TestReflectionUtil.SetField(_compass, "playerTransform", _playerGo.transform);
        TestReflectionUtil.SetField(_compass, "stripRect", _stripRect);
        TestReflectionUtil.SetField(_compass, "northMarker", _northRect);
        TestReflectionUtil.SetField(_compass, "eastMarker", _eastRect);
        TestReflectionUtil.SetField(_compass, "southMarker", _southRect);
        TestReflectionUtil.SetField(_compass, "westMarker", _westRect);
        TestReflectionUtil.SetField(_compass, "halfFovDegrees", HalfFov);
    }

    [TearDown]
    public void TearDown()
    {
        if (_go != null) Object.DestroyImmediate(_go);
        if (_playerGo != null) Object.DestroyImmediate(_playerGo);
        if (_stripGo != null) Object.DestroyImmediate(_stripGo);
        if (_northGo != null) Object.DestroyImmediate(_northGo);
        if (_eastGo != null) Object.DestroyImmediate(_eastGo);
        if (_southGo != null) Object.DestroyImmediate(_southGo);
        if (_westGo != null) Object.DestroyImmediate(_westGo);
    }

    private void SetYaw(float yawDegrees)
    {
        _playerGo.transform.rotation = Quaternion.Euler(0f, yawDegrees, 0f);
    }

    [Test]
    public void Update_NullPlayerTransform_DoesNotThrow_AndDoesNotTouchMarkers()
    {
        TestReflectionUtil.SetField(_compass, "playerTransform", null);
        _northRect.gameObject.SetActive(false);

        Assert.DoesNotThrow(() => TestReflectionUtil.InvokeMethod(_compass, "Update"));
        Assert.IsFalse(_northRect.gameObject.activeSelf, "Guard clause must return before touching any marker.");
    }

    [Test]
    public void Update_NullStripRect_DoesNotThrow()
    {
        TestReflectionUtil.SetField(_compass, "stripRect", null);

        Assert.DoesNotThrow(() => TestReflectionUtil.InvokeMethod(_compass, "Update"));
    }

    [Test]
    public void Update_YawZero_NorthVisibleAtCentre_EastSouthWestHidden()
    {
        SetYaw(0f);

        TestReflectionUtil.InvokeMethod(_compass, "Update");

        Assert.IsTrue(_northRect.gameObject.activeSelf);
        Assert.AreEqual(0f, _northRect.anchoredPosition.x, 0.001f);

        Assert.IsFalse(_eastRect.gameObject.activeSelf);
        Assert.IsFalse(_southRect.gameObject.activeSelf);
        Assert.IsFalse(_westRect.gameObject.activeSelf);
    }

    [Test]
    public void Update_YawNinety_EastVisibleAtCentre_NorthHidden()
    {
        SetYaw(90f);

        TestReflectionUtil.InvokeMethod(_compass, "Update");

        Assert.IsTrue(_eastRect.gameObject.activeSelf);
        Assert.AreEqual(0f, _eastRect.anchoredPosition.x, 0.001f);
        Assert.IsFalse(_northRect.gameObject.activeSelf);
    }

    [Test]
    public void Update_YawThirty_NorthVisible_OffsetMatchesCompassProjection()
    {
        SetYaw(30f);

        TestReflectionUtil.InvokeMethod(_compass, "Update");

        float halfWidth = StripWidth * 0.5f;
        float expectedX = CompassProjection.MarkerOffsetX(30f, 0f, HalfFov, halfWidth);

        Assert.IsTrue(_northRect.gameObject.activeSelf);
        Assert.AreEqual(expectedX, _northRect.anchoredPosition.x, 0.001f);
    }

    [Test]
    public void Update_MarkerJustOutsideFov_IsHiddenAndPositionUntouched()
    {
        // South bearing is 180 deg away from yaw 0 -- always outside a 60 deg half-FOV.
        var startPos = new Vector2(42f, 0f);
        _southRect.anchoredPosition = startPos;
        SetYaw(0f);

        TestReflectionUtil.InvokeMethod(_compass, "Update");

        Assert.IsFalse(_southRect.gameObject.activeSelf);
        Assert.AreEqual(startPos, _southRect.anchoredPosition, "Invisible markers must not have their position rewritten.");
    }

    [Test]
    public void PlaceMarker_NullMarker_DoesNotThrow()
    {
        Assert.DoesNotThrow(() =>
            TestReflectionUtil.InvokeMethod(_compass, "PlaceMarker", (object)null, 0f, 0f, 100f));
    }
}
