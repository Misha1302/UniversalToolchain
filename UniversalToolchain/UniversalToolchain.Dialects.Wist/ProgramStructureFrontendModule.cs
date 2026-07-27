using BasicCore.Contracts;
using BasicCore.ParserWrapper;
using BasicCore.Registration;
using BasicCore.TranslatorWrapper;
using BasicTypesExtensions;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Wist;

[DialectRuntimeExport("FrontendModule", "ProgramStructure")]
public sealed class ProgramStructureFrontendModule : IFrontendCoreModule
{
    public void InitAstTranslator(IAstToBytecodeTranslator translator)
    {
        translator.AddVisitors(new ProgramAstVisitor());
    }

    private sealed class ProgramAstVisitor : IAstVisitor
    {
        public void TryVisit(BytecodeVisitorData data)
        {
            if (data.Node.NodeType != ExtensibleEnum<AstNodeTag>.Get("Program"))
                return;

            foreach (var child in data.Node.Children)
                data.AstToBytecodeTranslator.Translate(child);
        }
    }
}