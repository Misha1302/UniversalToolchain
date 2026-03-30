namespace UniversalToolchain.Modules.Tests.ModuleCoverage;

[TestFixture]
public class CommentsModulePipelineTests
{
    private static readonly string[] Modules = ModulePipelineTestHelper.FullUniversalModules;
    [Test] public void Comments_SingleLineComment_IsIgnoredByExecution(){using var h=new ModulePipelineTestHelper();h.ExecuteEquivalent("// comment\n2+3","2+3",Modules);}    
    [Test] public void Comments_MultiLineComment_IsIgnoredByExecution(){using var h=new ModulePipelineTestHelper();h.ExecuteEquivalent("2 /*x*/ + 3","2 + 3",Modules);}    
    [Test] public void Comments_EndOfLineComment_DoesNotAffectNextStatement(){using var h=new ModulePipelineTestHelper();h.ExecuteEquivalent("let x = 2 // c\nx + 3","let x = 2\nx + 3",Modules);}    
    [Test] public void Comments_CommentOnlyLine_DoesNotCreatePhantomStatement(){using var h=new ModulePipelineTestHelper();h.ExecuteEquivalent("let x = 2\n//c\nx+3","let x = 2\nx+3",Modules);}    
    [Test] public void Comments_UnterminatedBlockComment_FailsDeterministically(){using var h=new ModulePipelineTestHelper();h.AssertFails("2 /* bad",Modules,"comment");}
}
