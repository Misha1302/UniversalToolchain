using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Core.Binding;

internal interface IDialectBindingSource
{
    DialectBindingInputKind InputKind { get; }

    string Name { get; }

    string? Version { get; }

    string? BaseDialectName { get; }

    IReadOnlyList<string> UseModules { get; }

    IReadOnlyList<string> ExcludeModules { get; }

    IReadOnlyList<OrderBindingDirectiveRecord> OrderRules { get; }

    IReadOnlyList<BackendBindingDirectiveRecord> BackendDirectives { get; }

    IReadOnlyList<IntrinsicBindingDirectiveRecord> IntrinsicDirectives { get; }

    IReadOnlyList<OptimizerBindingDirectiveRecord> OptimizerDirectives { get; }

    SecurityProfile? SecurityProfile { get; }

    IReadOnlyList<KeyValuePair<string, bool>> Capabilities { get; }
}