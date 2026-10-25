#if NETFRAMEWORK
// Enables C# init / required on .NET Framework 4.8
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit
    {
    }
}
#endif
