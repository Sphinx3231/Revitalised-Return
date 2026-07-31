using System.Reflection;

// Shared reflection helpers for reaching private serialized fields / private lifecycle
// methods on production MonoBehaviours from EditMode tests, without changing any
// production code's accessibility (Approach doc: read-only w.r.t. production code).
internal static class TestReflectionUtil
{
    private const BindingFlags InstanceAll = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    public static void SetField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, InstanceAll);
        if (field == null)
        {
            throw new System.Exception($"Field '{fieldName}' not found on {target.GetType()}");
        }
        field.SetValue(target, value);
    }

    public static T GetField<T>(object target, string fieldName)
    {
        var field = target.GetType().GetField(fieldName, InstanceAll);
        if (field == null)
        {
            throw new System.Exception($"Field '{fieldName}' not found on {target.GetType()}");
        }
        return (T)field.GetValue(target);
    }

    public static object InvokeMethod(object target, string methodName, params object[] args)
    {
        var method = target.GetType().GetMethod(methodName, InstanceAll);
        if (method == null)
        {
            throw new System.Exception($"Method '{methodName}' not found on {target.GetType()}");
        }
        return method.Invoke(target, args);
    }

    private const BindingFlags StaticAll = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    public static object InvokeStaticMethod(System.Type type, string methodName, params object[] args)
    {
        var method = type.GetMethod(methodName, StaticAll);
        if (method == null)
        {
            throw new System.Exception($"Static method '{methodName}' not found on {type}");
        }
        return method.Invoke(null, args);
    }
}
