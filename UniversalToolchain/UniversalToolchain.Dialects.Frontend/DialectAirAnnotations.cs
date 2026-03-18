using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Frontend;

public interface IDialectAirAnnotation;

public sealed record DialectNameAirAnnotation(string Name) : IDialectAirAnnotation;
public sealed record UseModulesAirAnnotation(IReadOnlyList<string> ModuleNames) : IDialectAirAnnotation;
public sealed record ExcludeModulesAirAnnotation(IReadOnlyList<string> ModuleNames) : IDialectAirAnnotation;
public sealed record FrontendOrderAirAnnotation(string SourceModule, string TargetModule) : IDialectAirAnnotation;
public sealed record MiddleEndOrderAirAnnotation(string SourceModule, string TargetModule) : IDialectAirAnnotation;
public sealed record BackendOrderAirAnnotation(string SourceModule, string TargetModule) : IDialectAirAnnotation;
public sealed record AllowedBackendsAirAnnotation(DialectBackendTarget Backend, bool Enabled) : IDialectAirAnnotation;
public sealed record RequiredIntrinsicsAirAnnotation(string Name, bool Allowed, DialectBackendTarget Target) : IDialectAirAnnotation;
public sealed record RequiredOptimizersAirAnnotation(string Name, bool Enabled, DialectBackendTarget Target) : IDialectAirAnnotation;
public sealed record SecurityModeAirAnnotation(DialectSecurityProfile SecurityProfile) : IDialectAirAnnotation;
public sealed record CapabilitiesAirAnnotation(string Name, bool Value) : IDialectAirAnnotation;
