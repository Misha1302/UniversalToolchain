using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Core.Binding;

internal readonly record struct BackendBindingDirectiveRecord(DialectBackendId Backend, bool Enabled);

internal readonly record struct IntrinsicBindingDirectiveRecord(string Name, DialectBackendSelector Target, bool Allowed);

internal readonly record struct OptimizerBindingDirectiveRecord(string Name, DialectBackendSelector Target, bool Enabled);

internal readonly record struct OrderBindingDirectiveRecord(OrderRuleKind Kind, string ModuleName, string RelatedModuleName);
