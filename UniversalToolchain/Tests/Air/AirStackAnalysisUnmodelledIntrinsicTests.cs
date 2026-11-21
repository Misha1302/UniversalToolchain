using IntermediateRepresentationAbstractions;
using UniversalToolchain.Air.Analysis;
using UniversalToolchain.Ssa.Lowering;

namespace Tests.Air;

/// <summary>
/// Diagnostics for an unmodelled AIR intrinsic (UT-A050 follow-up).
/// <para>
/// The SSA pack models a subset of the intrinsic universe. When it meets one it does not model, the honest
/// report is a single diagnostic naming that intrinsic. The stack analyzer used to emit a <b>second</b>,
/// fabricated diagnostic as well: an unmodelled intrinsic has an unknown stack effect, but the analyzer left
/// the operands on the stack and then complained that the terminal block "finishes with 2 evaluation-stack
/// values".
/// </para>
/// <para>
/// That second message is a consequence of the analyzer's own ignorance, not of the program, and it points
/// the author at a stack imbalance that does not exist. These tests pin the real cause and the absence of the
/// fabricated consequence.
/// </para>
/// </summary>
[TestFixture]
public sealed class AirStackAnalysisUnmodelledIntrinsicTests
{
    [Test]
    public void Analyze_WhenIntrinsicIsUnmodelled_ReportsTheCauseExactlyOnce()
    {
        // The AIR that '1 / 0' produces on the CIL backend: two operands and an intrinsic the core
        // arithmetic pack does not model. Before the fix this produced two diagnostics.
        var ir = new AbstractIR();
        ir.Push(1);
        ir.Push(0);
        ir.Intrinsic("div_i32");

        var graph = new AirControlFlowGraphBuilder().Build(ir.Instructions).Graph;
        var analysis = new AirStackAnalyzer(AirCoreIntrinsicDescriptors.ArithmeticInt32).Analyze(graph);

        Assert.Multiple(() =>
        {
            Assert.That(analysis.Diagnostics, Has.Count.EqualTo(1),
                "the unmodelled intrinsic is the cause; the stack-shape complaint is a fabricated " +
                "consequence of not modelling it:" + Environment.NewLine +
                string.Join(Environment.NewLine, analysis.Diagnostics));
            Assert.That(analysis.Diagnostics[0], Does.Contain("div_i32"),
                "the diagnostic must name the intrinsic the pack does not model");
        });
    }

    [Test]
    public void Analyze_WhenIntrinsicIsUnmodelled_DoesNotClaimAFalseStackImbalance()
    {
        var ir = new AbstractIR();
        ir.Push(1);
        ir.Push(0);
        ir.Intrinsic("div_i32");

        var graph = new AirControlFlowGraphBuilder().Build(ir.Instructions).Graph;
        var analysis = new AirStackAnalyzer(AirCoreIntrinsicDescriptors.ArithmeticInt32).Analyze(graph);

        Assert.That(analysis.Diagnostics, Has.None.Contains("evaluation-stack values"),
            "an unmodelled intrinsic has an unknown stack effect, so the analyzer cannot honestly claim " +
            "the terminal block has the wrong number of values");
    }

    [Test]
    public void Analyze_WhenArithmeticIsModelled_StillReportsATrueStackImbalance()
    {
        // The guard against over-suppression: a *modelled* intrinsic that genuinely leaves two values on the
        // stack is a real defect and must still be reported. Without this, the fix could silence the very
        // check it exists to keep honest.
        var ir = new AbstractIR();
        ir.Push(2);
        ir.Push(3);
        ir.Intrinsic(AirIntrinsicIds.AddInt32Unchecked);
        ir.Push(4);

        var graph = new AirControlFlowGraphBuilder().Build(ir.Instructions).Graph;
        var analysis = new AirStackAnalyzer(AirCoreIntrinsicDescriptors.ArithmeticInt32).Analyze(graph);

        Assert.That(analysis.Diagnostics, Has.Some.Contains("evaluation-stack values"),
            "a known intrinsic that leaves two values on the stack is a real imbalance and must be reported");
    }

    [Test]
    public void Analyze_WhenPushOnly_StaysClean()
    {
        // Baseline: the analyzer must not become noisy for legal programs.
        var ir = new AbstractIR();
        ir.Push(2);
        ir.Push(3);
        ir.Intrinsic(AirIntrinsicIds.AddInt32Unchecked);

        var graph = new AirControlFlowGraphBuilder().Build(ir.Instructions).Graph;
        var analysis = new AirStackAnalyzer(AirCoreIntrinsicDescriptors.ArithmeticInt32).Analyze(graph);

        Assert.That(analysis.Diagnostics, Is.Empty,
            string.Join(Environment.NewLine, analysis.Diagnostics));
    }

    [Test]
    public void Analyze_WhenSupportedIntrinsicFollowsAnUnmodelledOne_DoesNotInventEmptyStack()
    {
        // The state is unknown after 'div_i32', so a following supported intrinsic cannot be simulated.
        // TryPop on an unknown state fails, and without a guard that failure is reported as "consumes an
        // empty stack" - a second fabricated diagnostic, the same defect class as the terminal-stack one.
        var ir = new AbstractIR();
        ir.Push(1);
        ir.Push(0);
        ir.Intrinsic("div_i32");
        ir.Push(3);
        ir.Intrinsic(AirIntrinsicIds.AddInt32Unchecked);

        var graph = new AirControlFlowGraphBuilder().Build(ir.Instructions).Graph;
        var analysis = new AirStackAnalyzer(AirCoreIntrinsicDescriptors.ArithmeticInt32).Analyze(graph);

        Assert.Multiple(() =>
        {
            Assert.That(analysis.Diagnostics, Has.None.Contains("empty stack"),
                "an unknown state is not an empty stack:" + Environment.NewLine +
                string.Join(Environment.NewLine, analysis.Diagnostics));
            Assert.That(analysis.Diagnostics, Has.Exactly(1).Contains("div_i32"),
                "the unmodelled intrinsic is reported once, as the cause");
        });
    }

    [Test]
    public void Analyze_WhenDropFollowsAnUnmodelledOne_DoesNotInventUnderflow()
    {
        // Same defect class through a different opcode: Drop pops, the pop hits an unknown state, and
        // "underflow" would be a fabricated fact about a stack the analyzer never computed.
        var ir = new AbstractIR();
        ir.Push(1);
        ir.Push(0);
        ir.Intrinsic("div_i32");
        ir.Drop();

        var graph = new AirControlFlowGraphBuilder().Build(ir.Instructions).Graph;
        var analysis = new AirStackAnalyzer(AirCoreIntrinsicDescriptors.ArithmeticInt32).Analyze(graph);

        Assert.That(analysis.Diagnostics, Has.None.Contains("underflow"),
            string.Join(Environment.NewLine, analysis.Diagnostics));
    }

    [Test]
    public void Analyze_WhenConditionalJumpFollowsAnUnmodelledOne_DoesNotInventAnEmptyStack()
    {
        var merge = Guid.Parse("aaaaaaaa-1111-2222-3333-444455556666");
        var ir = new AbstractIR();
        ir.Push(1);
        ir.Push(0);
        ir.Intrinsic("div_i32");
        ir.SetLabel(merge);
        ir.JmpIf(merge);

        var graph = new AirControlFlowGraphBuilder().Build(ir.Instructions).Graph;
        var analysis = new AirStackAnalyzer(AirCoreIntrinsicDescriptors.ArithmeticInt32).Analyze(graph);

        Assert.That(analysis.Diagnostics, Has.None.Contains("consumes an empty stack"),
            string.Join(Environment.NewLine, analysis.Diagnostics));
    }

    [Test]
    public void Analyze_WhenPushHasAnUnsupportedValueType_LeavesTheStateUnknown()
    {
        // Asserted on the state itself, not on a downstream diagnostic: an unsupported push has an unknown
        // stack effect, and any later instruction judged against the pre-push shape would be judged against a
        // shape this program never has. A weaker test that only looks for a terminal complaint passes even
        // when the state is silently kept, which is exactly what a surviving mutant showed.
        var ir = new AbstractIR();
        ir.Push(42);
        ir.Push(Guid.Empty);

        var graph = new AirControlFlowGraphBuilder().Build(ir.Instructions).Graph;
        var analysis = new AirStackAnalyzer(AirCoreIntrinsicDescriptors.ArithmeticInt32).Analyze(graph);

        Assert.Multiple(() =>
        {
            Assert.That(analysis.ExitStates.Values, Has.Some.Matches<AirStackState>(static s => s.IsUnknown),
                "an unsupported pushed type has an unknown stack effect");
            Assert.That(analysis.Diagnostics, Has.Some.Contains("unsupported value type"),
                "the unsupported type is named as the cause");
        });
    }

    [Test]
    public void Analyze_WhenUnknownStateMergesWithAKnownState_KeepsTheKnownStateWithoutComplaining()
    {
        // Two predecessors reach the same block: one lost track (unmodelled intrinsic), the other has a
        // computable shape. Losing track is not a disagreement. Treating it as one fabricates an
        // "incompatible AIR stack state" report, and dropping the known state would lose a real check.
        var merge = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
        var join = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002");

        var ir = new AbstractIR();
        ir.Push(true);
        ir.JmpIf(merge);
        ir.Push(1);
        ir.Push(0);
        ir.Intrinsic("div_i32");
        ir.Jmp(join);
        ir.SetLabel(merge);
        ir.Push(7);
        ir.SetLabel(join);
        ir.Drop();

        var graph = new AirControlFlowGraphBuilder().Build(ir.Instructions).Graph;
        var analysis = new AirStackAnalyzer(AirCoreIntrinsicDescriptors.ArithmeticInt32).Analyze(graph);

        Assert.Multiple(() =>
        {
            Assert.That(analysis.Diagnostics, Has.None.Contains("Incompatible AIR stack state"),
                "an unknown predecessor is not a conflicting shape:" + Environment.NewLine +
                string.Join(Environment.NewLine, analysis.Diagnostics));
            Assert.That(analysis.EntryStates.Values, Has.None.Matches<AirStackState>(static s => s.IsUnknown),
                "the known shape wins the merge, so downstream checks still have a fact to work with");
        });
    }

    [Test]
    public void Analyze_UnknownIsDistinguishableFromEmpty()
    {
        // The three-valued pop result exists so a caller cannot confuse "I do not know" with "the stack is
        // empty". This pins the distinction itself, not its consequences.
        Assert.Multiple(() =>
        {
            Assert.That(AirStackState.Unknown.TryPop(out _, out var unknownNext), Is.EqualTo(AirStackPopResult.Unknown));
            Assert.That(unknownNext.IsUnknown, Is.True);
            Assert.That(AirStackState.Empty.TryPop(out _, out _), Is.EqualTo(AirStackPopResult.Underflow));
            Assert.That(new AirStackState([AirValueTypes.Int32]).TryPop(out var type, out _), Is.EqualTo(AirStackPopResult.Popped));
            Assert.That(type, Is.EqualTo(AirValueTypes.Int32));
            Assert.That(AirStackState.Unknown, Is.Not.EqualTo(AirStackState.Empty));
        });
    }

    [Test]
    public void Analyze_WhenKnownStateArrivesBeforeTheUnknownOne_KeepsTheKnownState()
    {
        // The mirror image of the previous test. Merge order is decided by the CFG walk, so both orderings
        // matter: an unknown state must never *overwrite* a known shape either. A one-sided test passes while
        // half the rule is inverted.
        var unknownL = Guid.Parse("cccccccc-0000-0000-0000-000000000001");
        var end = Guid.Parse("cccccccc-0000-0000-0000-000000000009");

        var ir = new AbstractIR();
        ir.Push(true);
        ir.JmpIf(unknownL);
        ir.Push(7);
        ir.Jmp(end);
        ir.SetLabel(unknownL);
        ir.Push(1);
        ir.Push(0);
        ir.Intrinsic("div_i32");
        ir.SetLabel(end);
        ir.Drop();

        var graph = new AirControlFlowGraphBuilder().Build(ir.Instructions).Graph;
        var analysis = new AirStackAnalyzer(AirCoreIntrinsicDescriptors.ArithmeticInt32).Analyze(graph);

        // The join block is the single terminal block; nothing else in this CFG has no successors.
        var join = graph.Blocks.Single(static b => b.Terminator.Successors.Count == 0).Id;

        Assert.Multiple(() =>
        {
            Assert.That(analysis.EntryStates[join].IsUnknown, Is.False,
                "the known predecessor arrived first and must not be replaced by an unknown state");
            Assert.That(analysis.EntryStates[join].Types, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void Analyze_WhenOpcodeIsOutsideTheModelledSet_LeavesTheStateUnknown()
    {
        // An unmodelled opcode has an unknown stack effect for the same reason an unmodelled intrinsic does.
        // `0x7F` is not in the UOpCode enumeration; the analyzer must report it and stop reasoning about the
        // stack instead of attributing the untouched operands to whatever comes next.
        var ir = new AbstractIR();
        ir.Push(1);
        ir.AppendInstructions([new Instruction((UOpCode)0x7F)]);

        var graph = new AirControlFlowGraphBuilder().Build(ir.Instructions).Graph;
        var analysis = new AirStackAnalyzer(AirCoreIntrinsicDescriptors.ArithmeticInt32).Analyze(graph);

        Assert.Multiple(() =>
        {
            Assert.That(analysis.Diagnostics, Has.Some.Contains("unsupported opcode"),
                "the unmodelled opcode is named as the cause");
            Assert.That(analysis.ExitStates.Values, Has.Some.Matches<AirStackState>(static s => s.IsUnknown),
                "an unmodelled opcode has an unknown stack effect");
        });
    }

    [Test]
    public void Analyze_WhenIntrinsicPayloadIsInvalid_LeavesTheStateUnknown()
    {
        // The third way the analyzer can lose track of the stack: the intrinsic instruction carries no
        // structured invocation, so neither the intrinsic nor its arity is known. The same rule applies -
        // report the cause once and stop drawing conclusions about the stack.
        var ir = new AbstractIR();
        ir.Push(1);
        ir.AppendInstructions([new Instruction(UOpCode.Intrinsic)]);

        var graph = new AirControlFlowGraphBuilder().Build(ir.Instructions).Graph;
        var analysis = new AirStackAnalyzer(AirCoreIntrinsicDescriptors.ArithmeticInt32).Analyze(graph);

        Assert.Multiple(() =>
        {
            Assert.That(analysis.Diagnostics, Has.Some.Contains("invalid payload"),
                "the malformed instruction is named as the cause");
            Assert.That(analysis.ExitStates.Values, Has.Some.Matches<AirStackState>(static s => s.IsUnknown),
                "an unreadable intrinsic has an unknown stack effect");
        });
    }

    [Test]
    public void Analyze_WhenDataOperandCountMismatches_LeavesTheStateUnknown()
    {
        // A declared descriptor that disagrees with the instruction's payload is a fourth way the analyzer
        // cannot know the stack effect: it neither consumes the modelled operands nor produces the modelled
        // results. Keeping the previous state would attribute both to the next instruction.
        var set = new AirIntrinsicDescriptorSet(
        [
            new AirIntrinsicDescriptor(
                "needs_one_operand",
                [AirValueTypes.Int32],
                [AirValueTypes.Int32],
                dataOperandCount: 1)
        ]);

        var ir = new AbstractIR();
        ir.Push(5);
        ir.Intrinsic("needs_one_operand");

        var graph = new AirControlFlowGraphBuilder().Build(ir.Instructions).Graph;
        var analysis = new AirStackAnalyzer(set).Analyze(graph);

        Assert.Multiple(() =>
        {
            Assert.That(analysis.Diagnostics, Has.Some.Contains("data operands"),
                "the arity disagreement is named as the cause");
            Assert.That(analysis.ExitStates.Values, Has.Some.Matches<AirStackState>(static s => s.IsUnknown),
                "an instruction whose arity disagrees with its descriptor has an unknown stack effect");
        });
    }

    [Test]
    public void Analyze_WhenPushCarriesNoOperand_LeavesTheStateUnknown()
    {
        // A Push with no operand has no computable effect: it pushes nothing the analyzer can name, so the
        // shape after it is unknown rather than "whatever it was before".
        var ir = new AbstractIR();
        ir.Push(1);
        ir.AppendInstructions([new Instruction(UOpCode.Push)]);

        var graph = new AirControlFlowGraphBuilder().Build(ir.Instructions).Graph;
        var analysis = new AirStackAnalyzer(AirCoreIntrinsicDescriptors.ArithmeticInt32).Analyze(graph);

        Assert.Multiple(() =>
        {
            Assert.That(analysis.Diagnostics, Has.Some.Contains("expected 1"),
                "the malformed push is named as the cause");
            Assert.That(analysis.ExitStates.Values, Has.Some.Matches<AirStackState>(static s => s.IsUnknown),
                "a push without an operand has an unknown stack effect");
        });
    }
}
