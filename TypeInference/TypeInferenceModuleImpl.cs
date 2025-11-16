// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using BasicCore;
using BasicCore.ParserWrapper;

namespace TypeInference;

public class TypeInferenceModuleImpl : ICoreModule
{
    public void InitParser(IParser parser)
    {
        // parser.Configuration.NodeCreators.Add(100f, new TypeInferenceNodeCreator());
    }

    public AstNode ProcessAst(AstNode astRoot)
    {
        var typeInferer = new TypeInferer();
        return typeInferer.InferTypes(astRoot);
    }

    public void RegisterTypeInferenceRule(ITypeInferenceRule rule)
    {
        // Для расширения системы новыми правилами вывода типов
    }
}