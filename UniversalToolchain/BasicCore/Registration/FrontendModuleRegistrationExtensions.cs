namespace BasicCore.Registration;

public static class FrontendModuleRegistrationExtensions
{
    public static void AddLexemes(this ILexer lexer, params IReadOnlyList<LexemeRegistration>[] registrations)
    {
        foreach (var registrationSet in registrations)
        foreach (var registration in registrationSet)
        {
            lexer.Configuration.TryAddPattern(
                new LexemePattern(registration.Pattern, LexemeType.CreateOrGet(registration.Tag)),
                registration.Ignore,
                registration.Priority
            );
        }
    }

    public static void AddNodeCreators(this IParser parser, params IReadOnlyList<NodeCreatorRegistration>[] registrations)
    {
        foreach (var registrationSet in registrations)
        foreach (var registration in registrationSet)
            parser.Configuration.NodeCreators.Add(registration.Priority, registration.Creator);
    }

    public static void AddVisitors(this IAstToBytecodeTranslator translator, params IAstVisitor[] visitors)
    {
        foreach (var visitor in visitors)
            translator.Configuration.Visitors.Add(visitor);
    }
}

public readonly record struct LexemeRegistration(string Pattern, string Tag, bool Ignore = false, float Priority = 0f);

public readonly record struct NodeCreatorRegistration(float Priority, IAstNodeCreator Creator);