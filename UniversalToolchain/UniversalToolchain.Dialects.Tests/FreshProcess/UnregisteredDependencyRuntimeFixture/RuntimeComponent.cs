using HostOnlyContractFixture;

namespace UnregisteredDependencyRuntimeFixture;

public sealed class RuntimeComponent : IHostOnlyContract
{
    public string Value => "runtime";
}
