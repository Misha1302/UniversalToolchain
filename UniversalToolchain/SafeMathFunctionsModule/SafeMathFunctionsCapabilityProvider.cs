namespace SafeMathFunctionsModule;

public sealed class SafeMathFunctionsCapabilityProvider :
    ILanguageFeatureDescriptorProvider,
    IBuiltinFunctionDescriptorProvider,
    IBuiltinFunctionRuntimeBindingProvider
{
    private static readonly LanguageFeatureId FeatureId = new("SafeMathFunctions");
    private static readonly FunctionTypeDescriptor Number = new("number");

    public IReadOnlyList<LanguageFeatureDescriptor> GetLanguageFeatures()
    {
        return
        [
            new LanguageFeatureDescriptor(
                FeatureId,
                "Safe math functions",
                LanguageFeatureKind.FunctionSet,
                ["SafeMathFunctions"],
                [],
                [
                    new("abs", LanguageFeatureSymbolKind.Function, "abs(number value) -> number", "Returns the absolute value."),
                    new("clamp", LanguageFeatureSymbolKind.Function, "clamp(number value, number min, number max) -> number", "Clamps a numeric value into an inclusive range."),
                    new("max", LanguageFeatureSymbolKind.Function, "max(number left, number right) -> number", "Returns the greater numeric value."),
                    new("min", LanguageFeatureSymbolKind.Function, "min(number left, number right) -> number", "Returns the smaller numeric value.")
                ],
                ["cil", "interpreter"],
                "Provides pure numeric helper functions owned by the SafeMathFunctions module.")
        ];
    }

    public IReadOnlyList<BuiltinFunctionDescriptor> GetFunctions()
    {
        return
        [
            CreatePureDescriptor("abs", [new FunctionParameterDescriptor("value", Number)]),
            CreatePureDescriptor(
                "clamp",
                [
                    new FunctionParameterDescriptor("value", Number),
                    new FunctionParameterDescriptor("min", Number),
                    new FunctionParameterDescriptor("max", Number)
                ]),
            CreatePureDescriptor(
                "max",
                [
                    new FunctionParameterDescriptor("left", Number),
                    new FunctionParameterDescriptor("right", Number)
                ]),
            CreatePureDescriptor(
                "min",
                [
                    new FunctionParameterDescriptor("left", Number),
                    new FunctionParameterDescriptor("right", Number)
                ])
        ];
    }

    public IReadOnlyList<BuiltinFunctionRuntimeBinding> GetRuntimeBindings()
    {
        return
        [
            CreateStaticMethodBinding("abs", [Number], nameof(SafeMathFunctions.Abs)),
            CreateStaticMethodBinding("clamp", [Number, Number, Number], nameof(SafeMathFunctions.Clamp)),
            CreateStaticMethodBinding("max", [Number, Number], nameof(SafeMathFunctions.Max)),
            CreateStaticMethodBinding("min", [Number, Number], nameof(SafeMathFunctions.Min))
        ];
    }

    private static BuiltinFunctionDescriptor CreatePureDescriptor(
        string name,
        IReadOnlyList<FunctionParameterDescriptor> parameters)
    {
        return new BuiltinFunctionDescriptor(
            name,
            FeatureId,
            parameters,
            Number,
            FunctionPurity.Pure,
            ["cil", "interpreter"]);
    }

    private static BuiltinFunctionRuntimeBinding CreateStaticMethodBinding(
        string name,
        IReadOnlyList<FunctionTypeDescriptor> parameterTypes,
        string methodName)
    {
        return new BuiltinFunctionRuntimeBinding(
            new BuiltinFunctionSignature(name, parameterTypes),
            Number,
            FeatureId,
            typeof(SafeMathFunctions).GetMethod(methodName, BindingFlags.Public | BindingFlags.Static)!,
            ["cil", "interpreter"]);
    }
}
