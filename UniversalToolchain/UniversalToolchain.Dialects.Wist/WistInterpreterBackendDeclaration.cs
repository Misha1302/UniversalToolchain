using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Wist;

[DialectBackendAlias("interpreter")]
[DialectRuntimeExport("wist", "Backend", "interpreter")]
internal sealed class WistInterpreterBackendDeclaration : DialectBackendDeclaration
{
    public override DialectBackendId BackendId => WistDialectBackendIds.Interpreter;
}
