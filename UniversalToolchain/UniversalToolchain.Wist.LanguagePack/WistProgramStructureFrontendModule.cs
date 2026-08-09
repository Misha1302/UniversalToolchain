using BasicCore.Contracts;
using BasicCore.ParserWrapper;
using BasicCore.Registration;
using BasicCore.TranslatorWrapper;
using BasicTypesExtensions;
using UniversalToolchain.ModuleContracts;

namespace UniversalToolchain.Wist.LanguagePack;

/// <summary>
/// Direct-runtime infrastructure module for the Wist program root. It owns no feature selection or
/// ordering decisions and is always prepended after LanguagePlan has selected the semantic modules.
/// </summary>
internal sealed class WistProgramStructureFrontendModule : IFrontendCoreModule, IModuleContractDescriptorProvider
{

    public IReadOnlyList<IModuleContractFacet> GetFacets() =>
    [
        new VerifierContractFacet(new ModuleId("wist.program-structure"), [])
    ];

    public void InitAstTranslator(IAstToBytecodeTranslator translator)
    {
        ArgumentNullException.ThrowIfNull(translator);
        translator.AddVisitors(new ProgramAstVisitor());
    }

    private sealed class ProgramAstVisitor : IAstVisitor
    {
        public void TryVisit(BytecodeVisitorData data)
        {
            ArgumentNullException.ThrowIfNull(data);
            if (data.Node.NodeType != ExtensibleEnum<AstNodeTag>.Get("Program"))
                return;

            foreach (var child in data.Node.Children)
                data.AstToBytecodeTranslator.Translate(child);
        }
    }
}
