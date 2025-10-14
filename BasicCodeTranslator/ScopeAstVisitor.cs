// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using BasicParser;
using BasicTypesExtensions;

namespace BasicCodeTranslator;

public class ScopeAstVisitor : IAstVisitor
{
    public void TryVisit(VisitorData data)
    {
        if (data.Node.NodeType != ExtensibleEnum<AstNodeTag>.Get("Scope")) return;

        foreach (var child in data.Node.Children)
            data.Translator.Translate(child);
    }
}