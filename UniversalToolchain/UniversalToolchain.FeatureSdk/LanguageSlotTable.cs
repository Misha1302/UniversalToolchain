using UniversalToolchain.Language.Abstractions;

namespace UniversalToolchain.FeatureSdk;

/// <summary>
/// Declares how many contributions a slot may contain in a compiled plan, and why.
/// <para>
/// The project law forbids a second parser, a second binder or a second runtime provider. Before this
/// table that law was enforced only where a package happened to remember to declare
/// <c>LanguageSlotMultiplicity.Single</c>; <c>Many</c> is the default, so an external package that said
/// nothing could select a second parser and the plan stayed silent. Convention cannot enforce a law.
/// </para>
/// </summary>
public sealed record LanguageSlotDescriptor(
    LanguageSlotId Slot,
    LanguageSlotMultiplicity Multiplicity,
    string Rationale)
{
    public bool IsSingleOwner => Multiplicity == LanguageSlotMultiplicity.Single;
}

/// <summary>
/// The single declared answer to "how many owners does this slot have".
/// <para>
/// This is a property of the <b>slot</b>, not of each contribution. A package cannot opt itself out of a
/// law by forgetting a constructor argument, which is exactly what the old per-contribution flag allowed.
/// </para>
/// <para>
/// Slots absent from the table are <see cref="LanguageSlotMultiplicity.Many"/>. That is the honest
/// default: the planner must not invent restrictions for slots it knows nothing about, because a language
/// author may define their own slot with any cardinality their design needs.
/// </para>
/// </summary>
public static class LanguageSlotTable
{
    private static readonly IReadOnlyDictionary<LanguageSlotId, LanguageSlotDescriptor> Declared =
        new[]
        {
            new LanguageSlotDescriptor(
                LanguageSlots.FrontendParser,
                LanguageSlotMultiplicity.Single,
                "A language has one front end. Two parsers would make the entry semantics depend on which one " +
                "the planner happened to reach first."),

            new LanguageSlotDescriptor(
                LanguageSlots.SemanticsBinding,
                LanguageSlotMultiplicity.Single,
                "A language has one binder. Two binders would produce two different semantic programs from the " +
                "same syntax tree, and nothing downstream could choose between them."),

            new LanguageSlotDescriptor(
                LanguageSlots.RuntimeProvider,
                LanguageSlotMultiplicity.Single,
                "A plan is executed by one runtime provider. Two providers would make the execution contract " +
                "ambiguous at the point where it is finally resolved.")
        }
        .ToDictionary(static descriptor => descriptor.Slot);

    /// <summary>
    /// The declared owners of every standard slot, in ordinal order. Used by documentation gates and by
    /// tests that must cover the whole table rather than the parts someone remembered.
    /// </summary>
    public static IReadOnlyList<LanguageSlotDescriptor> All { get; } =
        Declared.Values.OrderBy(static descriptor => descriptor.Slot.Value, StringComparer.Ordinal).ToArray();

    /// <summary>
    /// Describes <paramref name="slot"/>. Undeclared slots are many-owner, which is what a language author
    /// who invented a slot expects.
    /// </summary>
    public static LanguageSlotDescriptor Describe(LanguageSlotId slot) =>
        Declared.TryGetValue(slot, out var declared)
            ? declared
            : new LanguageSlotDescriptor(slot, LanguageSlotMultiplicity.Many, "Undeclared slot: no cardinality law applies.");

    /// <summary>True when the slot is declared exactly-one-owner by the project, not by a package.</summary>
    public static bool IsDeclaredSingleOwner(LanguageSlotId slot) =>
        Declared.TryGetValue(slot, out var declared) && declared.IsSingleOwner;
}
