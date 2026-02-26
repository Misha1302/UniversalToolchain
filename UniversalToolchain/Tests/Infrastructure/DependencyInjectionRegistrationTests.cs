using BasicCore.Contracts;
using Tests.DependencyInjection.Nested.ArithmeticModule;
using Tests.Infrastructure;

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

        public interface ITestDependency;
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