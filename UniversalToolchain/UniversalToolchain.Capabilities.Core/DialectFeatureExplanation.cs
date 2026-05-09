using ExceptionsManager;
using System.Collections.ObjectModel;
using UniversalToolchain.Capabilities.Abstractions;
using UniversalToolchain.Functions.Abstractions;

namespace UniversalToolchain.Capabilities.Core;

public sealed class DialectFeatureExplanation
{
    private readonly ReadOnlyCollection<LanguageFeatureDescriptor> _availableFeatures;
    private readonly ReadOnlyCollection<BuiltinFunctionDescriptor> _availableFunctions;
    private readonly ReadOnlyCollection<LanguageFeatureSymbolDescriptor> _availableSymbols;
    private readonly ReadOnlyCollection<string> _backendSupport;
    private readonly ReadOnlyCollection<UnavailableFeatureExplanation> _unavailableKnownFeatures;

    public DialectFeatureExplanation(
        string dialectName,
        IEnumerable<LanguageFeatureDescriptor> availableFeatures,
        IEnumerable<UnavailableFeatureExplanation> unavailableKnownFeatures,
        IEnumerable<LanguageFeatureSymbolDescriptor> availableSymbols,
        IEnumerable<BuiltinFunctionDescriptor> availableFunctions,
        IEnumerable<string> backendSupport)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dialectName);
        availableFeatures = availableFeatures.ArgNotNull();
        unavailableKnownFeatures = unavailableKnownFeatures.ArgNotNull();
        availableSymbols = availableSymbols.ArgNotNull();
        availableFunctions = availableFunctions.ArgNotNull();
        backendSupport = backendSupport.ArgNotNull();

        DialectName = dialectName;
        _availableFeatures = new ReadOnlyCollection<LanguageFeatureDescriptor>(availableFeatures.ToList());
        _unavailableKnownFeatures = new ReadOnlyCollection<UnavailableFeatureExplanation>(unavailableKnownFeatures.ToList());
        _availableSymbols = new ReadOnlyCollection<LanguageFeatureSymbolDescriptor>(availableSymbols.ToList());
        _availableFunctions = new ReadOnlyCollection<BuiltinFunctionDescriptor>(availableFunctions.ToList());
        _backendSupport = new ReadOnlyCollection<string>(backendSupport.ToList());
    }

    public string DialectName { get; }

    public IReadOnlyList<LanguageFeatureDescriptor> AvailableFeatures => _availableFeatures;

    public IReadOnlyList<UnavailableFeatureExplanation> UnavailableKnownFeatures => _unavailableKnownFeatures;

    public IReadOnlyList<LanguageFeatureSymbolDescriptor> AvailableSymbols => _availableSymbols;

    public IReadOnlyList<BuiltinFunctionDescriptor> AvailableFunctions => _availableFunctions;

    public IReadOnlyList<string> BackendSupport => _backendSupport;

    public sealed record UnavailableFeatureExplanation(
        LanguageFeatureDescriptor Feature,
        IReadOnlyList<string> Reasons);
}