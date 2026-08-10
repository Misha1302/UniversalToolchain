using BasicCore.Registration;
using UniversalToolchain.Dialects.Abstractions;

namespace ParametersSetterModule;

/// <summary>
///     Exports external parameter binding support into dialect composition.
/// </summary>
[DialectComponentContract("FrontendModule", "ParametersSetter")]
[AutoRegisterService]
public sealed class ParametersSetterModuleImpl : IFrontendCoreModule
{
    private static readonly IReadOnlyList<NodeCreatorRegistration> _nodeCreatorRegistrations =
    [
        new(100f, new ParametersSetNodeCreator())
    ];

    public void InitParser(IParser parser) => parser.AddNodeCreators(_nodeCreatorRegistrations);
}