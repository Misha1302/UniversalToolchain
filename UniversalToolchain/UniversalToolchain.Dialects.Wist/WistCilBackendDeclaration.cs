using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Wist;

[DialectBackendAlias("compiler")]
[DialectBackendRegistrarType(typeof(WistCilDialectBackendServiceProvider))]
[DialectRuntimeExport("Backend", "cil")]
[DialectRuntimeAlias("compiler")]
internal sealed class WistCilBackendDeclaration : DialectBackendDeclaration
{
    public override DialectBackendId BackendId => WistDialectBackendIds.Cil;
}