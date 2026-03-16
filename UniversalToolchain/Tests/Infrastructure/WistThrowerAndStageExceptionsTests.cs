namespace Tests.Infrastructure;

[TestFixture]
public class WistThrowerAndStageExceptionsTests
{
    [Test]
    public void LexerFactory_ShouldThrowLexerException_WithStageLocationAndMessage()
    {
        var location = new SourceLocation { Line = 12, Column = 7, File = "test.wist" };
        var ex = Assert.Throws<LexerException>(() => WistThrower.Lexer("bad token", location));

        Assert.That(ex, Is.TypeOf<LexerException>());
        Assert.That(ex!.Stage, Is.EqualTo("Lexer"));
        Assert.That(ex.Location, Is.EqualTo(location));
        Assert.That(ex.Message, Is.EqualTo("bad token"));
    }

    [Test]
    public void ParserFactory_ShouldThrowParserException_WithStageAndMessage()
    {
        var ex = Assert.Throws<ParserException>(() => WistThrower.Parser("parse fail"));

        Assert.That(ex, Is.TypeOf<ParserException>());
        Assert.That(ex!.Stage, Is.EqualTo("Parser"));
        Assert.That(ex.Message, Is.EqualTo("parse fail"));
    }

    [Test]
    public void ParserFactory_WithLocation_ShouldPreserveLocation()
    {
        var location = new SourceLocation { Line = 2, Column = 10 };
        var ex = Assert.Throws<ParserException>(() => WistThrower.Parser("parse fail", location));

        Assert.That(ex, Is.TypeOf<ParserException>());
        Assert.That(ex!.Stage, Is.EqualTo("Parser"));
        Assert.That(ex.Location, Is.EqualTo(location));
    }

    [Test]
    public void ImportFactory_ShouldThrowImportException_WithImportStage()
    {
        var ex = Assert.Throws<ImportException>(() => WistThrower.Import("import fail"));

        Assert.That(ex, Is.TypeOf<ImportException>());
        Assert.That(ex!.Stage, Is.EqualTo("Import"));
        Assert.That(ex.Message, Is.EqualTo("import fail"));
    }

    [Test]
    public void RuntimeFactory_ShouldThrowRuntimeExecutionException_WithInnerExceptionPreserved()
    {
        var inner = new InvalidOperationException("inner");
        var ex = Assert.Throws<RuntimeExecutionException>(() => WistThrower.Runtime("runtime fail", inner));

        Assert.That(ex, Is.TypeOf<RuntimeExecutionException>());
        Assert.That(ex!.Stage, Is.EqualTo("Runtime"));
        Assert.That(ex.InnerException, Is.SameAs(inner));
    }

    [Test]
    public void InternalCompilerFactory_ShouldThrowInternalCompilerException_WithStage()
    {
        var ex = Assert.Throws<InternalCompilerException>(() => WistThrower.InternalCompiler("compiler fail"));

        Assert.That(ex, Is.TypeOf<InternalCompilerException>());
        Assert.That(ex!.Stage, Is.EqualTo("InternalCompiler"));
    }



    [Test]
    public void LexerException_Constructor_ShouldSetLexerStage()
    {
        var ex = new LexerException("boom", new SourceLocation { Line = 1, Column = 1 });

        Assert.That(ex.Stage, Is.EqualTo("Lexer"));
        Assert.That(ex.Message, Is.EqualTo("boom"));
    }

    [TestCase(typeof(ParserException), "Parser")]
    [TestCase(typeof(BytecodeGenerationException), "Bytecode")]
    [TestCase(typeof(RuntimeExecutionException), "Runtime")]
    [TestCase(typeof(TypeSystemException), "TypeSystem")]
    [TestCase(typeof(ImportException), "Import")]
    [TestCase(typeof(InternalCompilerException), "InternalCompiler")]
    public void StageException_Constructors_ShouldSetExpectedStage(Type exceptionType, string expectedStage)
    {
        var instance = (WistException)Activator.CreateInstance(exceptionType, "boom")!;

        Assert.That(instance.Stage, Is.EqualTo(expectedStage));
        Assert.That(instance.Message, Is.EqualTo("boom"));
    }
}
