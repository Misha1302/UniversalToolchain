namespace UniversalToolchain.Dialects.Tests;

public class FrameworkNativeExtensionSeamsTests
{
    [Test]
    public void DiBuiltInComposition_IsDeterministic_AndUsesStagedOrdering()
    {
        var first = DialectDslTestComposition.CreateRegistry();
        var second = DialectDslTestComposition.CreateRegistry();
        var parserCreators = DialectDslParserNodeRegistry.CreateRegistrations(first).Select(x => x.Creator.GetType().Name).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(first.DirectiveFeatures.Select(x => (x.Id, x.Keyword, x.ParserOrder.Slot, x.ParserOrder.Sequence)),
                Is.EqualTo(second.DirectiveFeatures.Select(x => (x.Id, x.Keyword, x.ParserOrder.Slot, x.ParserOrder.Sequence))));
            Assert.That(first.DirectiveFeatures.Select(x => x.Keyword), Is.EqualTo(new[]
            {
                "use", "exclude", "requires", "before", "after", "backend", "allow", "forbid", "enable", "disable", "security", "capability"
            }));
            Assert.That(first.DirectiveFeatures.Select(x => x.ParserOrder.Slot), Is.EqualTo(new[]
            {
                DialectDirectiveSlot.ModuleSelection,
                DialectDirectiveSlot.ModuleSelection,
                DialectDirectiveSlot.ModuleOrdering,
                DialectDirectiveSlot.ModuleOrdering,
                DialectDirectiveSlot.ModuleOrdering,
                DialectDirectiveSlot.BackendSelection,
                DialectDirectiveSlot.IntrinsicPolicy,
                DialectDirectiveSlot.IntrinsicPolicy,
                DialectDirectiveSlot.OptimizerPolicy,
                DialectDirectiveSlot.OptimizerPolicy,
                DialectDirectiveSlot.Security,
                DialectDirectiveSlot.Capabilities
            }));
            Assert.That(parserCreators, Does.Contain(nameof(FeatureDialectDirectiveNodeCreator)));
        });
    }

    [Test]
    public void CustomDirectiveFeature_CanBeAddedThroughProviderRegistration_WithoutEditingCentralDirectivePlumbing()
    {
        var registry = DialectDslTestComposition.CreateRegistry(services => services.AddDialectDirectiveFeatureProvider<AliasDirectiveFeatureProvider>());
        var compiler = DialectDslTestComposition.CreateCompiler(services => services.AddDialectDirectiveFeatureProvider<AliasDirectiveFeatureProvider>());

        var slice = compiler.Compile(
            """
            dialect Tiny
            alias math arithmetic
            use arithmetic
            """);

        Assert.Multiple(() =>
        {
            Assert.That(slice.Name, Is.EqualTo("Tiny"));
            Assert.That(slice.UseModules, Is.EqualTo(new[] { "arithmetic" }));
            Assert.That(slice.CapabilityDirectives.Select(x => (x.Name, x.Value)), Is.EqualTo(new[]
            {
                ("alias:math->arithmetic", true)
            }));
            Assert.That(registry.DirectiveFeatures.Select(x => x.Keyword), Does.Contain("alias"));
        });
    }

    [Test]
    public void CustomDirectiveFeature_CanBeAddedThroughFeatureRegistration()
    {
        var compiler = DialectDslTestComposition.CreateCompiler(services => services.AddDialectDirectiveFeature<DirectAliasDirectiveFeature>());

        var slice = compiler.Compile(
            """
            dialect Tiny
            direct-alias math arithmetic
            """);

        Assert.That(slice.CapabilityDirectives.Select(x => x.Name), Is.EqualTo(new[] { "direct-alias:math->arithmetic" }));
    }

    [Test]
    public void SemanticBinder_BindsCompiledSliceIntoNormalizedDialectDefinition()
    {
        var slice = DialectDslTestComposition.CreateCompiler().Compile(
            """
            dialect Tiny
            use B,A
            exclude Legacy
            before A,B
            backend interpreter,cil
            allow add_i32
            forbid sub_i32
            enable Ssa
            disable Fold
            capability sandbox
            """);

        var diagnostics = new List<DialectDiagnostic>();
        var definition = DialectDefinitionSemanticBinder.Bind(slice, diagnostics);

        Assert.Multiple(() =>
        {
            Assert.That(diagnostics, Is.Empty);
            Assert.That(definition.Name, Is.EqualTo("Tiny"));
            Assert.That(definition.ModulePolicy.IncludedModules, Is.EqualTo(new[] { "A", "B" }));
            Assert.That(definition.ModulePolicy.ExcludedModules, Is.EqualTo(new[] { "Legacy" }));
            Assert.That(definition.BackendPolicy.EnabledBackends, Is.EqualTo(new[] { TestBackendIds.Cil, TestBackendIds.Interpreter }));
            Assert.That(definition.IntrinsicPolicy.AllowedIntrinsics, Is.EqualTo(new[] { "add_i32" }));
            Assert.That(definition.IntrinsicPolicy.ForbiddenIntrinsics, Is.EqualTo(new[] { "sub_i32" }));
            Assert.That(definition.OptimizerPolicy.EnabledOptimizers, Is.EqualTo(new[] { "Ssa" }));
            Assert.That(definition.OptimizerPolicy.DisabledOptimizers, Is.EqualTo(new[] { "Fold" }));
            Assert.That(definition.SecurityPolicy, Is.Null);
            Assert.That(definition.OrderRules.Select(x => (x.Kind, x.ModuleName, x.RelatedModuleName)), Is.EqualTo(new[]
            {
                (OrderRuleKind.Before, "A", "B")
            }));
        });
    }

    [Test]
    public void RuntimeDescriptorFactory_BuildsDeterministically_FromProviders()
    {
        var first = DialectRuntimeDescriptorRegistryFactory.BuildFromAssemblies(Assembly.GetExecutingAssembly());
        var second = DialectRuntimeDescriptorRegistryFactory.BuildFromAssemblies(Assembly.GetExecutingAssembly());

        Assert.Multiple(() =>
        {
            Assert.That(first.Modules.Keys, Is.EqualTo(second.Modules.Keys));
            Assert.That(first.Backends.Keys, Is.EqualTo(second.Backends.Keys));
            Assert.That(first.Optimizers.Keys, Is.EqualTo(second.Optimizers.Keys));
            Assert.That(first.Intrinsics.Keys, Is.EqualTo(second.Intrinsics.Keys));
            Assert.That(first.Modules.Keys, Does.Contain("DemoFrontend"));
            Assert.That(first.Optimizers.Keys, Does.Contain("DemoOptimizer"));
            Assert.That(first.Intrinsics.Keys, Does.Contain(("add_i32", TestBackendIds.Any)));
            Assert.That(first.Backends.Keys, Does.Contain(TestBackendIds.Interpreter));
        });
    }

    [Test]
    public void BuildPlanBuilder_ProjectsParserSemanticModel_WithScopedTargets()
    {
        var document = new DialectSyntaxDocument(
            "Scoped",
            null,
            ["A"],
            [],
            [],
            [new BackendDirectiveSyntax(TestBackendIds.Interpreter, true)],
            [new IntrinsicDirectiveSyntax("add_i32", true, TestBackendIds.InterpreterSelector)],
            [new OptimizerDirectiveSyntax("Ssa", true, TestBackendIds.Any)],
            null,
            []);

        var plan = new DialectBuildPlanBuilder().Build(document);

        Assert.Multiple(() =>
        {
            Assert.That(plan.CanBuild, Is.True);
            Assert.That(plan.IntrinsicDirectives.Select(x => (x.Name, x.Allowed, x.Target)), Is.EqualTo(new[]
            {
                ("add_i32", true, TestBackendIds.InterpreterSelector)
            }));
            Assert.That(plan.OptimizerDirectives.Select(x => (x.Name, x.Enabled, x.Target)), Is.EqualTo(new[]
            {
                ("Ssa", true, TestBackendIds.Any)
            }));
        });
    }

    private sealed class DemoRuntimeDescriptorProvider : IDialectRuntimeDescriptorProvider
    {
        public int Order => 10;

        public void Register(DialectRuntimeDescriptorRegistryBuilder builder)
        {
            builder
                .RegisterModule(new RuntimeModuleDescriptor("DemoFrontend", typeof(FakeFrontendModule)))
                .RegisterBackend(new RuntimeBackendDescriptor(TestBackendIds.Interpreter, "InterpreterBackend"))
                .RegisterOptimizer(new RuntimeOptimizerDescriptor("DemoOptimizer", typeof(FakeOptimizerModule)))
                .RegisterIntrinsic(new RuntimeIntrinsicDescriptor("add_i32", TestBackendIds.Any));
        }
    }

    private sealed class AliasDirectiveFeatureProvider : IDialectDslFeatureProvider
    {
        public int Order => 100;

        public void Register(DialectDslRegistryBuilder builder)
        {
            builder.RegisterFeature(new AliasDirectiveFeature("alias", "tests.alias", "alias:"));
        }
    }

    private sealed class DirectAliasDirectiveFeature : AliasDirectiveFeature
    {
        public DirectAliasDirectiveFeature() : base("direct-alias", "tests.direct-alias", "direct-alias:")
        {
        }
    }

    private class AliasDirectiveFeature(string keyword, string id, string capabilityPrefix) : IDialectDirectiveFeature
    {
        private static readonly DialectListStateKey<string> AccumulationKey = new("AliasMappings");
        private static readonly DialectSetStateKey<string> ValidationKey = new("AliasMappings", StringComparer.Ordinal);

        public string Id => id;

        public string Keyword => keyword;

        public string LexemeTag => $"DialectDirectiveKeyword.{keyword}";

        public DialectDirectiveParserOrder ParserOrder => new(DialectDirectiveSlot.Extension, 0);

        public bool IsSingleton => false;

        public string SingletonViolationMessage => $"Directive '{keyword}' can only be declared once.";

        public DialectDirectiveAstNode ParseDirective(AstNode lineNode)
        {
            if (lineNode.Children.Count != 3)
                DialectDefinitionSliceParseErrors.Fail($"Directive '{keyword}' expects exactly two identifiers.", lineNode.Children[0].LexemeValue);

            var source = lineNode.Children[1];
            var target = lineNode.Children[2];
            if (!DialectLexemeTags.IsTag(source.LexemeValue, DialectLexemeTags.Identifier) || !DialectLexemeTags.IsTag(target.LexemeValue, DialectLexemeTags.Identifier))
                DialectDefinitionSliceParseErrors.Fail($"Directive '{keyword}' expects identifier arguments.", source.LexemeValue ?? target.LexemeValue);

            return new DialectDirectiveAstNode(this, lineNode.Children[0].LexemeValue,
            [
                new AliasDirectivePayloadAstNode(new IdentifierValueAstNode(source.LexemeValue!), new IdentifierValueAstNode(target.LexemeValue!))
            ]);
        }

        public void Accumulate(IReadOnlyList<LexemeValue> line, DialectDirectiveAccumulation accumulation)
        {
            if (line.Count != 3)
                DialectDefinitionSliceParseErrors.Fail($"Directive '{keyword}' expects exactly two identifiers.", line[0]);

            accumulation.GetOrCreateList(AccumulationKey).Add($"{line[1].Text}->{line[2].Text}");
        }

        public void ValidateSemantic(DialectDirectiveAstNode directive, DialectDirectiveValidationContext context)
        {
            var payload = GetPayload(directive);
            if (string.Equals(payload.Source.Identifier, payload.Target.Identifier, StringComparison.Ordinal))
                DialectDefinitionSliceParseErrors.Fail("Alias source and target must differ.", directive.LexemeValue);

            context.AddValue(ValidationKey, $"{payload.Source.Identifier}->{payload.Target.Identifier}", "Duplicate alias directive is not allowed.", directive.LexemeValue);
        }

        public IReadOnlyList<IDialectDefinitionSliceAnnotation> Lower(DialectDirectiveAstNode directive)
        {
            var payload = GetPayload(directive);
            return [new CapabilityAirAnnotation([$"{capabilityPrefix}{payload.Source.Identifier}->{payload.Target.Identifier}"])];
        }

        private static AliasDirectivePayloadAstNode GetPayload(DialectDirectiveAstNode directive) =>
            directive.Payload as AliasDirectivePayloadAstNode
            ?? throw new ArgumentException("Alias directive payload is invalid.", nameof(directive));
    }

    private sealed class AliasDirectivePayloadAstNode : DialectAstNode
    {
        private static readonly AstNodeType PayloadNodeType = AstNodeType.CreateOrGet("AliasDirectivePayload");

        public AliasDirectivePayloadAstNode(IdentifierValueAstNode source, IdentifierValueAstNode target) : base(PayloadNodeType, null, [source, target])
        {
        }

        public IdentifierValueAstNode Source => (IdentifierValueAstNode)Children[0];

        public IdentifierValueAstNode Target => (IdentifierValueAstNode)Children[1];
    }

    private sealed class FakeFrontendModule : IFrontendCoreModule
    {
        public void InitLexer(ILexer lexer)
        {
        }

        public void InitParser(IParser parser)
        {
        }

        public AstNode ProcessAst(AstNode astRoot) => astRoot;

        public void InitAstTranslator(IAstToBytecodeTranslator translator)
        {
        }
    }

    private sealed class FakeOptimizerModule : IIRProcessingModule
    {
        public IAbstractIR Process(IAbstractIR air) => air;
    }
}