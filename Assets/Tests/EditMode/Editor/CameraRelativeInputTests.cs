using NUnit.Framework;
using UnityEngine;

// Charter 4.1's explicit amendment: flatten BEFORE normalize. See CameraRelativeInput.cs.
public class CameraRelativeInputTests
{
    private GameObject _cameraGo;

    [TearDown]
    public void TearDown()
    {
        if (_cameraGo != null)
        {
            Object.DestroyImmediate(_cameraGo);
        }
    }

    [Test]
    public void IdentityRotation_ForwardInput_PassesThroughUnchanged()
    {
        _cameraGo = new GameObject("Cam");
        _cameraGo.transform.rotation = Quaternion.identity;

        Vector3 result = CameraRelativeInput.ToCameraRelative(new Vector2(0f, 1f), _cameraGo.transform);

        AssertVector3Approximately(new Vector3(0f, 0f, 1f), result, 0.0001f);
        Assert.AreEqual(1f, result.magnitude, 0.0001f);
    }

    [Test]
    public void IdentityRotation_RightInput_PassesThroughUnchanged()
    {
        _cameraGo = new GameObject("Cam");
        _cameraGo.transform.rotation = Quaternion.identity;

        Vector3 result = CameraRelativeInput.ToCameraRelative(new Vector2(1f, 0f), _cameraGo.transform);

        AssertVector3Approximately(new Vector3(1f, 0f, 0f), result, 0.0001f);
    }

    [Test]
    public void Rotated90DegreesAroundY_ForwardInputBecomesWorldRight()
    {
        _cameraGo = new GameObject("Cam");
        _cameraGo.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

        Vector3 result = CameraRelativeInput.ToCameraRelative(new Vector2(0f, 1f), _cameraGo.transform);

        AssertVector3Approximately(new Vector3(1f, 0f, 0f), result, 0.0001f);
        Assert.AreEqual(1f, result.magnitude, 0.0001f);
    }

    [Test]
    public void PitchedCamera_OutputIsFlattenedAndStillUnitLength()
    {
        // Pitching the camera down 45deg is exactly the scenario the charter's own amendment
        // warns about: normalizing the raw camera-basis vector BEFORE flattening would leave
        // a shortened (sub-1.0-magnitude) horizontal result. Flatten-then-normalize must not.
        _cameraGo = new GameObject("Cam");
        _cameraGo.transform.rotation = Quaternion.Euler(45f, 0f, 0f);

        Vector3 result = CameraRelativeInput.ToCameraRelative(new Vector2(0f, 1f), _cameraGo.transform);

        Assert.AreEqual(0f, result.y, 0.0001f, "Result must be flattened to the XZ plane.");
        Assert.AreEqual(1f, result.magnitude, 0.0001f, "Pitch must not shorten the horizontal magnitude.");
        AssertVector3Approximately(new Vector3(0f, 0f, 1f), result, 0.0001f);
    }

    [Test]
    public void DiagonalInput_IsNormalized()
    {
        _cameraGo = new GameObject("Cam");
        _cameraGo.transform.rotation = Quaternion.identity;

        Vector3 result = CameraRelativeInput.ToCameraRelative(new Vector2(1f, 1f), _cameraGo.transform);

        Assert.AreEqual(1f, result.magnitude, 0.0001f);
    }

    [Test]
    public void ZeroInput_ReturnsZeroVector()
    {
        _cameraGo = new GameObject("Cam");
        _cameraGo.transform.rotation = Quaternion.identity;

        Vector3 result = CameraRelativeInput.ToCameraRelative(Vector2.zero, _cameraGo.transform);

        Assert.AreEqual(Vector3.zero, result);
    }

    private static void AssertVector3Approximately(Vector3 expected, Vector3 actual, float tolerance)
    {
        Assert.AreEqual(expected.x, actual.x, tolerance);
        Assert.AreEqual(expected.y, actual.y, tolerance);
        Assert.AreEqual(expected.z, actual.z, tolerance);
    }
}
