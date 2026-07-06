using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Core.Binding;

public readonly record struct BackendBindingDirectiveRecord(DialectBackendId Backend, bool Enabled);

public readonly record struct IntrinsicBindingDirectiveRecord(string Name, DialectBackendSelector Target, bool Allowed);

public readonly record struct OptimizerBindingDirectiveRecord(string Name, DialectBackendSelector Target, bool Enabled);

public readonly record struct OrderBindingDirectiveRecord(OrderRuleKind Kind, string ModuleName, string RelatedModuleName);
