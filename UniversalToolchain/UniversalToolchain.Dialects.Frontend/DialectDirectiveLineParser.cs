using BasicCore.LexerWrapper;
using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Frontend;

public sealed class DialectDirectiveLineParser
{
    public bool TryParse(IReadOnlyList<LexemeValue> line, DialectDirectiveAccumulation accumulation)
    {
        if (line == null)
        {
            Thrower.ArgumentNull(nameof(line));
        }

        if (accumulation == null)
        {
            Thrower.ArgumentNull(nameof(accumulation));
        }

        if (line.Count == 0)
        {
            return true;
        }

        if (TryParseUseExclude(line, accumulation))
        {
            return true;
        }

        if (TryParseOrderDirective(line, accumulation.OrderDirectives))
        {
            return true;
        }

        if (TryParseBackendDirective(line, accumulation.BackendDirectives))
        {
            return true;
        }

        if (TryParseIntrinsicDirective(line, accumulation.IntrinsicDirectives))
        {
            return true;
        }

        if (TryParseOptimizerDirective(line, accumulation.OptimizerDirectives))
        {
            return true;
        }

        if (TryParseSecurityDirective(line, accumulation))
        {
            return true;
        }

        if (TryParseCapabilityDirective(line, accumulation.CapabilityDirectives))
        {
            return true;
        }

        return false;
    }

    private static bool TryParseUseExclude(IReadOnlyList<LexemeValue> line, DialectDirectiveAccumulation accumulation)
    {
        if (line.Count != 2)
        {
            return false;
        }

        if (DialectLexemeTags.IsTag(line[0], DialectLexemeTags.UseKeyword))
        {
            if (!DialectLexemeTags.IsTag(line[1], DialectLexemeTags.Identifier))
            {
                DialectDefinitionSliceParseErrors.Fail("Expected module name after 'use'.", line[1]);
            }

            accumulation.UseModules.Add(line[1].Text);
            return true;
        }

        if (DialectLexemeTags.IsTag(line[0], DialectLexemeTags.ExcludeKeyword))
        {
            if (!DialectLexemeTags.IsTag(line[1], DialectLexemeTags.Identifier))
            {
                DialectDefinitionSliceParseErrors.Fail("Expected module name after 'exclude'.", line[1]);
            }

            accumulation.ExcludeModules.Add(line[1].Text);
            return true;
        }

        return false;
    }

    private static bool TryParseOrderDirective(IReadOnlyList<LexemeValue> line, List<DialectOrderDirective> directives)
    {
        if (!DialectLexemeTags.IsTag(line[0], DialectLexemeTags.RequiresKeyword) &&
            !DialectLexemeTags.IsTag(line[0], DialectLexemeTags.BeforeKeyword) &&
            !DialectLexemeTags.IsTag(line[0], DialectLexemeTags.AfterKeyword))
        {
            return false;
        }

        if (line.Count != 4)
        {
            DialectDefinitionSliceParseErrors.Fail("Expected order directive format: requires|before|after <Left> -> <Right>.", line[0]);
        }

        if (!DialectLexemeTags.IsTag(line[1], DialectLexemeTags.Identifier) ||
            !DialectLexemeTags.IsTag(line[2], DialectLexemeTags.ArrowToken) ||
            !DialectLexemeTags.IsTag(line[3], DialectLexemeTags.Identifier))
        {
            DialectDefinitionSliceParseErrors.Fail("Expected order directive format: requires|before|after <Left> -> <Right>.", line[0]);
        }

        directives.Add(new DialectOrderDirective(ParseOrderDirectiveKind(line[0]), line[1].Text, line[3].Text));
        return true;
    }

    private static DialectOrderDirectiveKind ParseOrderDirectiveKind(LexemeValue token)
    {
        if (DialectLexemeTags.IsTag(token, DialectLexemeTags.RequiresKeyword))
        {
            return DialectOrderDirectiveKind.Requires;
        }

        if (DialectLexemeTags.IsTag(token, DialectLexemeTags.BeforeKeyword))
        {
            return DialectOrderDirectiveKind.Before;
        }

        if (DialectLexemeTags.IsTag(token, DialectLexemeTags.AfterKeyword))
        {
            return DialectOrderDirectiveKind.After;
        }

        DialectDefinitionSliceParseErrors.Fail("Order directive must be requires|before|after.", token);
        return DialectOrderDirectiveKind.Requires;
    }

    private static bool TryParseBackendDirective(IReadOnlyList<LexemeValue> line, List<DialectBackendDirective> directives)
    {
        if (!DialectLexemeTags.IsTag(line[0], DialectLexemeTags.BackendKeyword))
        {
            return false;
        }

        if (line.Count != 3)
        {
            DialectDefinitionSliceParseErrors.Fail("Expected backend directive format: backend <interpreter|cil> enable|disable.", line[0]);
        }

        var target = ParseBackendTarget(line[1], allowAny: false);
        var enabled = ParseEnableDisable(line[2]);
        directives.Add(new DialectBackendDirective(target, enabled));
        return true;
    }

    private static bool TryParseIntrinsicDirective(IReadOnlyList<LexemeValue> line, List<DialectIntrinsicDirective> directives)
    {
        if (!DialectLexemeTags.IsTag(line[0], DialectLexemeTags.AllowKeyword) &&
            !DialectLexemeTags.IsTag(line[0], DialectLexemeTags.ForbidKeyword))
        {
            return false;
        }

        if (line.Count != 5)
        {
            DialectDefinitionSliceParseErrors.Fail("Expected intrinsic directive format: allow|forbid intrinsic \"name\" for <interpreter|cil|any>.", line[0]);
        }

        if (!DialectLexemeTags.IsTag(line[1], DialectLexemeTags.IntrinsicKeyword) ||
            !DialectLexemeTags.IsTag(line[2], DialectLexemeTags.StringLiteral) ||
            !DialectLexemeTags.IsTag(line[3], DialectLexemeTags.ForKeyword))
        {
            DialectDefinitionSliceParseErrors.Fail("Expected intrinsic directive format: allow|forbid intrinsic \"name\" for <interpreter|cil|any>.", line[0]);
        }

        var allow = DialectLexemeTags.IsTag(line[0], DialectLexemeTags.AllowKeyword);
        var name = Unquote(line[2].Text);
        var target = ParseBackendTarget(line[4], allowAny: true);
        directives.Add(new DialectIntrinsicDirective(name, allow, target));
        return true;
    }

    private static bool TryParseOptimizerDirective(IReadOnlyList<LexemeValue> line, List<DialectOptimizerDirective> directives)
    {
        if (!DialectLexemeTags.IsTag(line[0], DialectLexemeTags.EnableKeyword) &&
            !DialectLexemeTags.IsTag(line[0], DialectLexemeTags.DisableKeyword))
        {
            return false;
        }

        if (line.Count != 5)
        {
            DialectDefinitionSliceParseErrors.Fail("Expected optimizer directive format: enable|disable optimizer <Name> for <interpreter|cil|any>.", line[0]);
        }

        if (!DialectLexemeTags.IsTag(line[1], DialectLexemeTags.OptimizerKeyword) ||
            !DialectLexemeTags.IsTag(line[2], DialectLexemeTags.Identifier) ||
            !DialectLexemeTags.IsTag(line[3], DialectLexemeTags.ForKeyword))
        {
            DialectDefinitionSliceParseErrors.Fail("Expected optimizer directive format: enable|disable optimizer <Name> for <interpreter|cil|any>.", line[0]);
        }

        var enabled = DialectLexemeTags.IsTag(line[0], DialectLexemeTags.EnableKeyword);
        var target = ParseBackendTarget(line[4], allowAny: true);
        directives.Add(new DialectOptimizerDirective(line[2].Text, enabled, target));
        return true;
    }

    private static bool TryParseSecurityDirective(IReadOnlyList<LexemeValue> line, DialectDirectiveAccumulation accumulation)
    {
        if (!DialectLexemeTags.IsTag(line[0], DialectLexemeTags.SecurityKeyword))
        {
            return false;
        }

        if (line.Count != 2)
        {
            DialectDefinitionSliceParseErrors.Fail("Expected security directive format: security trusted|restricted.", line[0]);
        }

        if (accumulation.SecurityProfile != null)
        {
            DialectDefinitionSliceParseErrors.Fail("Security directive can be specified only once.", line[0]);
        }

        if (DialectLexemeTags.IsTag(line[1], DialectLexemeTags.TrustedKeyword))
        {
            accumulation.SecurityProfile = DialectSecurityProfile.Trusted;
            return true;
        }

        if (DialectLexemeTags.IsTag(line[1], DialectLexemeTags.RestrictedKeyword))
        {
            accumulation.SecurityProfile = DialectSecurityProfile.Restricted;
            return true;
        }

        DialectDefinitionSliceParseErrors.Fail("Security profile must be 'trusted' or 'restricted'.", line[1]);
        return true;
    }

    private static bool TryParseCapabilityDirective(IReadOnlyList<LexemeValue> line, List<DialectCapabilityDirective> directives)
    {
        if (!DialectLexemeTags.IsTag(line[0], DialectLexemeTags.CapabilityKeyword))
        {
            return false;
        }

        if (line.Count != 4)
        {
            DialectDefinitionSliceParseErrors.Fail("Expected capability directive format: capability <name> = true|false.", line[0]);
        }

        if (!DialectLexemeTags.IsTag(line[1], DialectLexemeTags.Identifier) ||
            !DialectLexemeTags.IsTag(line[2], DialectLexemeTags.EqualsToken))
        {
            DialectDefinitionSliceParseErrors.Fail("Expected capability directive format: capability <name> = true|false.", line[0]);
        }

        if (DialectLexemeTags.IsTag(line[3], DialectLexemeTags.TrueKeyword))
        {
            directives.Add(new DialectCapabilityDirective(line[1].Text, true));
            return true;
        }

        if (DialectLexemeTags.IsTag(line[3], DialectLexemeTags.FalseKeyword))
        {
            directives.Add(new DialectCapabilityDirective(line[1].Text, false));
            return true;
        }

        DialectDefinitionSliceParseErrors.Fail("Capability value must be true or false.", line[3]);
        return true;
    }

    private static DialectBackendTarget ParseBackendTarget(LexemeValue token, bool allowAny)
    {
        if (DialectLexemeTags.IsTag(token, DialectLexemeTags.InterpreterKeyword))
        {
            return DialectBackendTarget.Interpreter;
        }

        if (DialectLexemeTags.IsTag(token, DialectLexemeTags.CilKeyword))
        {
            return DialectBackendTarget.Cil;
        }

        if (allowAny && DialectLexemeTags.IsTag(token, DialectLexemeTags.AnyKeyword))
        {
            return DialectBackendTarget.Any;
        }

        var expected = allowAny ? "interpreter|cil|any" : "interpreter|cil";
        DialectDefinitionSliceParseErrors.Fail($"Backend target must be one of: {expected}.", token);
        return DialectBackendTarget.Any;
    }

    private static bool ParseEnableDisable(LexemeValue token)
    {
        if (DialectLexemeTags.IsTag(token, DialectLexemeTags.EnableKeyword))
        {
            return true;
        }

        if (DialectLexemeTags.IsTag(token, DialectLexemeTags.DisableKeyword))
        {
            return false;
        }

        DialectDefinitionSliceParseErrors.Fail("Expected enable|disable token.", token);
        return false;
    }

    private static string Unquote(string quoted)
    {
        if (quoted.Length >= 2 && quoted[0] == '"' && quoted[^1] == '"')
        {
            return quoted[1..^1];
        }

        Thrower.InvalidOpEx("Expected quoted string literal.");
        return string.Empty;
    }
}
