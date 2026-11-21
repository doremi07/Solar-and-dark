// Compile-check only: Unity's test runner assembly isn't in the reference DLLs, so stand in for the attribute.
namespace UnityEngine.TestTools
{
    [System.AttributeUsage(System.AttributeTargets.Method)]
    public class UnityTestAttribute : System.Attribute { }
}
