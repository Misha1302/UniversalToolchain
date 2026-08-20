using UniversalToolchain.ModuleContracts;

namespace NumbersModule.Contracts;

public sealed class NumbersModuleContractDescriptorProvider : IModuleContractDescriptorProvider
{
    public IReadOnlyList<ContractNamespaceOwner> NamespaceOwners => [ContractNamespaceOwner.Reserved("wist", "wist")];

    public IReadOnlyList<IModuleContractFacet> GetFacets() =>
    [
        new SyntaxContractFacet(
            NumbersContractIds.Module,
            [
                new LexemeContract(
                    "Number",
                    "Decimal numeric literal lexeme registered by NumbersModuleImpl.")
            ],
            []),
        new AstContractFacet(
            NumbersContractIds.Module,
            [
                new AstOwnershipContract(
                    NumbersContractIds.NumberNode,
                    AstOwnershipMode.Exclusive,
                    NumbersContractIds.Module,
                    [])
            ]),
        new BytecodeContractFacet(
            NumbersContractIds.Module,
            [
                new BytecodeEmissionContract(
                    NumbersContractIds.NumberNode,
                    [],
                    [NumbersContractIds.PushRealNumber],
                    new StackEffect(0, 1),
                    SideEffectPolicy.Pure)
            ]),
        new AirContractFacet(
            NumbersContractIds.Module,
            [
                new AirEmissionContract(
                    NumbersContractIds.PushRealNumber,
                    [KnownCoreAirPatterns.UniversalConstructorCall],
                    [KnownCoreIntrinsicSymbols.CallCSharpConstructor],
                    [
                        KnownCoreBackendCapabilities.UniversalCall,
                        KnownCoreBackendCapabilities.ObjectConstruction
                    ])
            ]),
        new CompilerFactOwnershipFacet(
            NumbersContractIds.Module,
            [
                new CompilerFactOwnershipContract(
                    NumbersFacts.NumericValuesSupported,
                    NumbersContractIds.Module)
            ]),
        new PipelineEffectFacet(
            NumbersContractIds.Module,
            [
                new PipelineEffectContract(
                    NumbersEffects.LowerNumericLiteral,
                    CompilerPipelineStage.Bytecode,
                    [KnownCoreCompilerFacts.AstBound],
                    [NumbersFacts.NumericValuesSupported],
                    [],
                    [KnownCoreCompilerFacts.BytecodeVerified])
            ])
    ];
}
