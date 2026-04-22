using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Wist;

[DialectBackendRegistrarType(typeof(WistInterpreterDialectBackendServiceProvider))]
[DialectBackendAlias("interpreter")]
[DialectRuntimeExport("Backend", "interpreter")]
internal sealed class WistInterpreterBackendDeclaration : DialectBackendDeclaration
{
    public override DialectBackendId BackendId => WistDialectBackendIds.Interpreter;
}
