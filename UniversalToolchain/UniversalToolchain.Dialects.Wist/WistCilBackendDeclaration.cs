using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Wist;

[DialectBackendRegistrarType(typeof(WistCilDialectBackendServiceProvider))]
[DialectRuntimeExport("Backend", "cil")]
internal sealed class WistCilBackendDeclaration : DialectBackendDeclaration
{
    public override DialectBackendId BackendId => WistDialectBackendIds.Cil;
}