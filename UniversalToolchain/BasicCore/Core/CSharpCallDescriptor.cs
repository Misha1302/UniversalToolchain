using System.Reflection;

namespace BasicCore.Core;

public sealed record CSharpCallDescriptor
{
    public CSharpCallDescriptor(MethodInfo method, CSharpCallReceiver receiver)
    {
        Method = method.ArgNotNull();
        Receiver = receiver.ArgNotNull();
    }

    public MethodInfo Method { get; }

    public CSharpCallReceiver Receiver { get; }
}

public abstract record CSharpCallReceiver
{
    public sealed record Static : CSharpCallReceiver;

    public sealed record ExecutionScopedProvider(Type ProviderType) : CSharpCallReceiver
    {
        public Type ProviderType { get; } = ProviderType.ArgNotNull();
    }
}
