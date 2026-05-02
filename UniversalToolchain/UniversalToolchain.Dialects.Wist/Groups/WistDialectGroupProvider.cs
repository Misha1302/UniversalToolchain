using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Wist.Groups;

public sealed class WistDialectGroupProvider : IDialectGroupProvider
{
    public IReadOnlyList<DialectGroupDescriptor> GetGroups() =>
    [
        new(
            "ArithmeticCore",
            [
                "Arithmetic",
                "Numbers",
                "Whitespaces"
            ],
            [
                new KeyValuePair<string, bool>("Arithmetic", true),
                new KeyValuePair<string, bool>("Numbers", true)
            ]),

        new(
            "ConditionsCore",
            [
                "BooleanConditions",
                "ComparisonConditions",
                "Conditions",
                "Equality"
            ],
            [
                new KeyValuePair<string, bool>("BooleanLogic", true),
                new KeyValuePair<string, bool>("Comparison", true),
                new KeyValuePair<string, bool>("Conditions", true)
            ]),

        new(
            "VariablesCore",
            [
                "Identifier",
                "Variables"
            ],
            [
                new KeyValuePair<string, bool>("Identifiers", true),
                new KeyValuePair<string, bool>("Variables", true)
            ]),

        new(
            "BlocksCore",
            [
                "Scopes",
                "SemicolonAsNewLine"
            ],
            [
                new KeyValuePair<string, bool>("Blocks", true)
            ]),

        new(
            "ControlFlowCore",
            [
                "Loops",
                "Labels"
            ],
            [
                new KeyValuePair<string, bool>("ControlFlow", true)
            ])
    ];
}