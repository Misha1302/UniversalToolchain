using BasicCodeTranslator.Visitors;
using System.Collections.Generic;

namespace BasicCodeTranslator;

public class BasicAstToBytecodeTranslatorImpl(BytecodeTranslatorConfiguration configuration) : IAstToBytecodeTranslator
{
    public BasicAstToBytecodeTranslatorImpl() : this(new BytecodeTranslatorConfiguration([]))
    {
    }

    public BytecodeTranslatorConfiguration Configuration { get; } = CreateConfiguration(configuration);

    public Bytecode Translate(AstNode root)
    {
        var bytecode = new Bytecode([]);
        var requestTranslator = new RequestTranslator(Configuration, bytecode);

        requestTranslator.Translate(root);

        return bytecode;
    }

    private static BytecodeTranslatorConfiguration CreateConfiguration(BytecodeTranslatorConfiguration configuration)
    {
        var visitors = new List<IAstVisitor>(configuration.Visitors.Count + 1);
        var hasStructuralScopeVisitor = false;

        foreach (var visitor in configuration.Visitors)
        {
            if (visitor is StructuralScopeAstVisitor)
                hasStructuralScopeVisitor = true;

            visitors.Add(visitor);
        }

        if (!hasStructuralScopeVisitor)
            visitors.Insert(0, new StructuralScopeAstVisitor());

        return new BytecodeTranslatorConfiguration(visitors);
    }

    private sealed class RequestTranslator(BytecodeTranslatorConfiguration configuration, Bytecode bytecode)
        : IAstToBytecodeTranslator
    {
        public BytecodeTranslatorConfiguration Configuration { get; } = configuration;

        public Bytecode Translate(AstNode root)
        {
            var data = new BytecodeVisitorData(this, bytecode, root);
            foreach (var visitor in Configuration.Visitors)
                visitor.TryVisit(data);

            return bytecode;
        }
    }
}
