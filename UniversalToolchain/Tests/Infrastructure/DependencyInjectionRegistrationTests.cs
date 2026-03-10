namespace Tests.Infrastructure
{
    [TestFixture]
    public class DependencyInjectionRegistrationTests
    {
        [Test]
        public void AddWistServices_RegistersExecutableGiversForDynamicMethodAndAbstractIr()
        {
            var services = new ServiceCollection();
            services.AddWistServices();

            using var provider = services.BuildServiceProvider();

            var dynamicMethodExecutableGivers = provider.GetServices<IExecutableGiver<DynamicMethod>>().ToList();
            var abstractIrExecutableGivers = provider.GetServices<IExecutableGiver<IAbstractIR>>().ToList();

            Assert.That(dynamicMethodExecutableGivers, Is.Not.Empty);
            Assert.That(abstractIrExecutableGivers, Is.Not.Empty);
        }

        [Test]
        public void RemoveAllByNamespace_RemovesServices_WhenNamespaceFilterMatchesSuffix()
        {
            var services = new ServiceCollection();
            services.AddSingleton<ITestDependency, TestDependency>();

            services.RemoveAllByNamespace("ArithmeticModule");

            Assert.That(services.Any(d => d.ImplementationType == typeof(TestDependency)), Is.False);
        }

        [Test]
        public void RemoveAllByNamespace_RemovesServices_WhenNamespaceFilterMatchesParentNamespace()
        {
            var services = new ServiceCollection();
            services.AddSingleton<ITestDependency, DependencyInjection.Nested.ArithmeticModule.Internal.TestDependency>();

            services.RemoveAllByNamespace("Tests.DependencyInjection.Nested.ArithmeticModule");

            Assert.That(services.Any(d => d.ImplementationType == typeof(DependencyInjection.Nested.ArithmeticModule.Internal.TestDependency)), Is.False);
        }

        [Test]
        public void AddAutoRegisteredServices_SkipsType_When_DefaultServiceTypeIsUnsupported()
        {
            var services = new ServiceCollection();

            services.AddAutoRegisteredServices(typeof(UnsupportedAutoRegistrationService).Assembly);

            Assert.That(services.Any(descriptor => descriptor.ImplementationType == typeof(UnsupportedAutoRegistrationService)), Is.False);
        }

        [Test]
        public void AddAutoRegisteredServices_UsesExplicitSupportedMapping_When_InterfaceIsKnown()
        {
            var services = new ServiceCollection();

            services.AddAutoRegisteredServices(typeof(FrontendModuleAutoRegistrationService).Assembly);

            Assert.That(services.Any(descriptor =>
                descriptor.ServiceType == typeof(IFrontendCoreModule) &&
                descriptor.ImplementationType == typeof(FrontendModuleAutoRegistrationService)), Is.True);
        }

        public interface ITestDependency;

        public interface IUnsupportedAutoRegistration;

        [AutoRegisterService]
        private sealed class UnsupportedAutoRegistrationService : IUnsupportedAutoRegistration;

        [AutoRegisterService]
        private sealed class FrontendModuleAutoRegistrationService : IFrontendCoreModule
        {
            public string ProcessText(string text) => text;
            public AstNode ProcessAst(AstNode root) => root;
            public Bytecode ProcessBytecode(Bytecode bytecode) => bytecode;

            public void InitLexer(ILexer lexer)
            {
            }

            public void InitParser(IParser parser)
            {
            }

            public void InitAstTranslator(IAstToBytecodeTranslator translator)
            {
            }
        }
    }
}

namespace Tests.DependencyInjection.Nested.ArithmeticModule
{
    public sealed class TestDependency : DependencyInjectionRegistrationTests.ITestDependency;
}

namespace Tests.DependencyInjection.Nested.ArithmeticModule.Internal
{
    public sealed class TestDependency : DependencyInjectionRegistrationTests.ITestDependency;
}