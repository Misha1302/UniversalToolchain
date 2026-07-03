using System;

namespace JetBrains.Annotations;

[AttributeUsage(AttributeTargets.All, Inherited = false)]
internal sealed class UsedImplicitlyAttribute : Attribute
{
    public UsedImplicitlyAttribute()
    {
    }
}
