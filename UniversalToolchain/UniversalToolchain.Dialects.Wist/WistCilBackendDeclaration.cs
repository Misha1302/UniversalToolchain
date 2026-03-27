using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Wist;

[DialectBackendAlias("compiler")]
[DialectRuntimeExport("wist", "Backend", "cil")]
[DialectRuntimeAlias("compiler")]
internal sealed class WistCilBackendDeclaration : DialectBackendDeclaration
{
    public override DialectBackendId BackendId => WistDialectBackendIds.Cil;
}
