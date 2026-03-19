namespace Tests.Infrastructure;

[TestFixture]
public class MethodsFinderAndTypesFinderTests
{
    [Test]
    public void Should_FindMethodByExactSignature_When_ParameterTypesProvided()
    {
        var method = MethodsFinder.GetMethod($"{typeof(OverloadTarget).FullName}.Echo", [typeof(int)]);

        Assert.That(method, Is.Not.Null);
        Assert.That(method!.GetParameters().Single().ParameterType, Is.EqualTo(typeof(int)));
    }

    [Test]
    public void Should_FindInstanceMethod_When_MethodIsNotStatic()
    {
        var method = MethodsFinder.GetMethod($"{typeof(OverloadTarget).FullName}.Increment", [typeof(int)]);

        Assert.That(method, Is.Not.Null);
        Assert.That(method!.IsStatic, Is.False);
    }

    [Test]
    public void Should_ResolveOverloadByParameterCount_When_SignatureNotProvided()
    {
        var method = MethodsFinder.GetMethod($"{typeof(OverloadTarget).FullName}.Echo", 2);

        Assert.That(method, Is.Not.Null);
        Assert.That(method!.GetParameters().Length, Is.EqualTo(2));
    }

    [Test]
    public void Should_ResolveGenericMethod_When_NoNonGenericAlternativeExists()
    {
        var method = MethodsFinder.GetMethod($"{typeof(GenericOnlyTarget).FullName}.Identity", 1);

        Assert.That(method, Is.Not.Null);
        Assert.That(method!.IsGenericMethodDefinition, Is.True);
    }

    [Test]
    public void Should_ReturnNull_When_MethodDoesNotExist()
    {
        var method = MethodsFinder.GetMethod($"{typeof(OverloadTarget).FullName}.NotExisting", [typeof(int)]);

        Assert.That(method, Is.Null);
    }

    [Test]
    public void Should_ReturnTypeByFullName_When_TypeExists()
    {
        var type = TypesFinder.GetType(typeof(OverloadTarget).FullName!);

        Assert.That(type, Is.EqualTo(typeof(OverloadTarget)));
    }

    [Test]
    public void Should_Throw_When_TypeDoesNotExist()
    {
        Assert.Throws<InvalidOperationException>(() => TypesFinder.GetType("Unknown.Type.Name"));
    }

    [Test]
    public void Should_IncludeRequestedTypeName_When_TypeDoesNotExist()
    {
        var missingTypeName = "Unknown.Type.Name.For.Contract";

        var ex = Assert.Throws<InvalidOperationException>(() => TypesFinder.GetType(missingTypeName));

        Assert.That(ex!.Message, Does.Contain($"Type '{missingTypeName}' was not found among loaded assemblies."));
    }

    [Test]
    public void RegisterAssembly_ShouldThrowArgumentNullException_When_AssemblyIsNull()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => TypesFinder.RegisterAssembly(null!));

        Assert.That(ex!.ParamName, Is.EqualTo("assembly"));
    }

    [Test]
    public void Should_ReportMethodPresence_When_ContainsAnyMethodIsCalled()
    {
        var contains = MethodsFinder.ContainsAnyMethod($"{typeof(OverloadTarget).FullName}.Echo");

        Assert.That(contains, Is.True);
    }

    public class OverloadTarget
    {
        public static int Echo(int value) => value;
        public static string Echo(string value) => value;
        public static string Echo(int a, int b) => $"{a}:{b}";
        public int Increment(int value) => value + 1;
    }

    public class GenericOnlyTarget
    {
        public static T Identity<T>(T value) => value;
    }
}