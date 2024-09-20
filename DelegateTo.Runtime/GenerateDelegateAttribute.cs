using System;

namespace DelegateTo;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
public sealed class GenerateDelegateAttribute : Attribute
{
    public string Prefix { get; set; } = string.Empty;

    /// <summary>
    /// Appends [MethodImplAttribute(MethodImplOptions.AggressiveInlining)] to generated members.
    /// </summary>
    public bool Inline { get; set; } = false;
}
