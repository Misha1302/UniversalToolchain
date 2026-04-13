using ExceptionsManager;

namespace UniversalToolchain.Dialects.Frontend;

public enum DialectParserStage
{
    LineSplitting = 0,
    Declaration = 1,
    Directives = 2,
    Document = 3
}

public enum DialectDirectiveSlot
{
    ModuleSelection = 0,
    ModuleOrdering = 1,
    BackendSelection = 2,
    IntrinsicPolicy = 3,
    OptimizerPolicy = 4,
    Security = 5,
    Capabilities = 6,
    Extension = 100
}

public readonly record struct DialectDirectiveParserOrder(DialectDirectiveSlot Slot, int Sequence) : IComparable<DialectDirectiveParserOrder>
{
    public int CompareTo(DialectDirectiveParserOrder other)
    {
        var slotComparison = Slot.CompareTo(other.Slot);
        if (slotComparison != 0)
            return slotComparison;

        return Sequence.CompareTo(other.Sequence);
    }

    public override string ToString() => $"{Slot}:{Sequence}";
}

public readonly record struct DialectParserOrder(DialectParserStage Stage, int Slot, int Sequence) : IComparable<DialectParserOrder>
{
    public int CompareTo(DialectParserOrder other)
    {
        var stageComparison = Stage.CompareTo(other.Stage);
        if (stageComparison != 0)
            return stageComparison;

        var slotComparison = Slot.CompareTo(other.Slot);
        if (slotComparison != 0)
            return slotComparison;

        return Sequence.CompareTo(other.Sequence);
    }

    public static DialectParserOrder Directive(DialectDirectiveParserOrder order) => new(DialectParserStage.Directives, (int)order.Slot, order.Sequence);

    public override string ToString() => $"{Stage}:{Slot}:{Sequence}";
}

public static class DialectParserOrders
{
    public static DialectParserOrder LineSplitter { get; } = new(DialectParserStage.LineSplitting, 0, 0);

    public static DialectParserOrder Declaration { get; } = new(DialectParserStage.Declaration, 0, 0);

    public static DialectParserOrder Document { get; } = new(DialectParserStage.Document, 0, 0);
}

internal static class DialectParserOrderValidation
{
    public static void EnsureNoCollisions<T>(IReadOnlyList<T> items, Func<T, DialectParserOrder> orderSelector, Func<T, string> describeItem, string scope)
    {
        items = items.ArgNotNull();

        var collision = items
            .GroupBy(orderSelector)
            .FirstOrDefault(group => group.Count() > 1);
        if (collision == null)
            return;

        var members = string.Join(", ", collision.Select(describeItem).OrderBy(x => x, StringComparer.Ordinal));
        Thrower.InvalidOpEx($"Dialect parser order collision in {scope} at '{collision.Key}'. Registered items: {members}.");
    }
}