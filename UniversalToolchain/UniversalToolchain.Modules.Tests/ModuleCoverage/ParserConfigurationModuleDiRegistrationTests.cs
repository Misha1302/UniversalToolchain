using ArithmeticModule.Module;
using BasicCore.Contracts;
using BasicParser.Core;
using ConditionsModule.Enums;
using ConditionsModule.Module;
using CSharpInteropModule.Module;
using EqualityModule;
using LabelsModule.Module;
using Microsoft.Extensions.DependencyInjection;
using ParserConfigurationModule.Core;
using ParserConfigurationModule.Module;
using ScopesModule.Module;
using VariablesModule;

namespace UniversalToolchain.Modules.Tests.ModuleCoverage;

[TestFixture]
public class ParserConfigurationModuleDiRegistrationTests
{
    private string _testConfigPath = string.Empty;

    [SetUp]
    public void SetUp()
    {
        _testConfigPath = Path.Combine(Path.GetTempPath(), $"parser_config_module_di_{Guid.NewGuid():N}.txt");
    }

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(_testConfigPath))
            File.Delete(_testConfigPath);
    }

    [Test]
    public void ParserOrderConfiguration_WithActionTypeDump_WritesParserCreatorOrderSnapshot()
    {
        using var services = CreateServiceProvider(ActionType.DumpConfiguration, _testConfigPath);
        var parser = CreateConfiguredParser(services);

        Assert.That(parser.Configuration.NodeCreators.SelectMany(static level => level.Value), Is.Not.Empty);
        Assert.That(File.Exists(_testConfigPath), Is.True);

        var dumpText = File.ReadAllText(_testConfigPath);
        Assert.That(dumpText, Does.Contain("# Parser Configuration Dump"));
        Assert.That(dumpText, Does.Contain("AdditionOperationNodeCreator"));
        Assert.That(dumpText, Does.Contain("ScopesCreator"));
    }

    [Test]
    public void ParserOrderConfiguration_WithActionTypeRead_AppliesConfiguredCreatorPriorities()
    {
        const string configuredAdditionPriority = "999.00|ArithmeticModule.Creators.AdditionOperationNodeCreator|0|Addition";
        const string configuredMultiplicationPriority = "-999.00|ArithmeticModule.Creators.MultiplicationOperationNodeCreator|0|Multiplication";

        File.WriteAllText(
            _testConfigPath,
            $$"""
              # parser order configuration injected via DI registration
              {{configuredAdditionPriority}}
              {{configuredMultiplicationPriority}}
              """);

        using var services = CreateServiceProvider(ActionType.ReadConfiguration, _testConfigPath);
        var parser = CreateConfiguredParser(services);

        Assert.That(GetCreatorPriority(parser, "AdditionOperationNodeCreator"), Is.EqualTo(999f));
        Assert.That(GetCreatorPriority(parser, "MultiplicationOperationNodeCreator"), Is.EqualTo(-999f));
    }

    private static ServiceProvider CreateServiceProvider(ActionType actionType, string configPath)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IFrontendCoreModule, ScopesModuleImpl>();
        services.AddSingleton<IFrontendCoreModule, ArithmeticModuleImpl>();
        services.AddSingleton<IFrontendCoreModule, VariablesModuleImpl>();
        services.AddSingleton<IFrontendCoreModule, LabelsModuleImpl>();
        services.AddSingleton<IFrontendCoreModule, EqualityModuleImpl>();
        services.AddSingleton<IFrontendCoreModule, CSharpInteropModuleImpl>();
        services.AddSingleton<IFrontendCoreModule, ConditionsModuleImpl>();
        services.AddSingleton<IFrontendCoreModule, ComparisonOperations>();
        services.AddSingleton<IFrontendCoreModule, BooleanOperations>();
        services.AddSingleton<IFrontendCoreModule>(_ => new ParserConfigurationModuleImpl(actionType, configPath));
        return services.BuildServiceProvider();
    }

    private static BasicParserImpl CreateConfiguredParser(ServiceProvider services)
    {
        var parser = new BasicParserImpl();
        foreach (var module in services.GetRequiredService<IEnumerable<IFrontendCoreModule>>())
            module.InitParser(parser);

        return parser;
    }

    private static float GetCreatorPriority(BasicParserImpl parser, string creatorTypeName)
    {
        foreach (var level in parser.Configuration.NodeCreators)
        {
            if (level.Value.Any(creator => creator.GetType().Name == creatorTypeName))
                return level.Key;
        }

        Assert.Fail($"Parser creator '{creatorTypeName}' was not registered.");
        return default;
    }
}
