using BasicCore.Capabilities;
using BasicCore.Core;
using BytecodeDynamicMethodsCompiler.Compilers;
using BytecodeDynamicMethodsCompiler.Core;
using IntermediateRepresentationAbstractions;

namespace UniversalToolchain.LanguageSdk.Generic.Tests;

/// <summary>
/// A registry descriptor carries <b>both</b> halves of an intrinsic: how to emit it
/// (<see cref="CilIntrinsicDescriptor.Compile"/>) and how it changes the type stack
/// (<see cref="CilIntrinsicDescriptor.ProcessTypes"/>).
/// </summary>
/// <remarks>
/// <para>
/// Only the first half was honoured. <c>AbstractMethodsIntrinsicCompiler.Compile</c> looks the descriptor
/// up and calls <c>descriptor.Compile</c>; <c>ProcessTypes</c> ignored the descriptor entirely and called
/// the static <c>IntrinsicTypeProcessor.ProcessTypes</c> dispatcher, which recognises the hardcoded symbol
/// families and throws <c>Unknown intrinsic capability</c> for anything else.
/// </para>
/// <para>
/// The consequence is not cosmetic. A descriptor's type-stack handler is the only place a non-core
/// intrinsic describes its stack effect, so ignoring it makes the type stack a closed universe even when
/// the registry is not: the emit half works, then type simulation rejects the instruction. A field that is
/// recorded and never consulted is worse than an absent one, because it reads as a supported contract.
/// </para>
/// </remarks>
[TestFixture]
public sealed class CilIntrinsicRegistryProcessTypesContractTests
{
    private const string ExternalIntrinsicName = "acme.saturating_add_i32";

    private static CilIntrinsicDescriptor Descriptor(
        string name,
        Action<Instruction, List<Type>> processTypes) =>
        new(name, static (_, _, _) => { }, processTypes);

    private static Instruction InstructionFor(string name) =>
        IntrinsicInstructionFactory.CreateForCapability(name);

    [Test]
    public void ProcessTypes_UsesTheDescriptorHandler_NotTheStaticDispatcher()
    {
        // The handler is deliberately distinguishable: it pushes a marker type no core intrinsic produces.
        // If the compiler consults the descriptor, the marker appears. If it calls the static dispatcher
        // instead, the call throws because the name belongs to no known family.
        var stack = new List<Type> { typeof(int), typeof(int) };
        var registry = new CilIntrinsicRegistry(
        [
            Descriptor(ExternalIntrinsicName, static (_, types) =>
            {
                types.RemoveRange(types.Count - 2, 2);
                types.Add(typeof(DateTime));
            })
        ]);

        new AbstractMethodsIntrinsicCompiler(registry).ProcessTypes(InstructionFor(ExternalIntrinsicName), stack);

        Assert.That(stack, Is.EqualTo(new List<Type> { typeof(DateTime) }),
            "the descriptor's own type-stack handler must be the one that runs");
    }

    [Test]
    public void ProcessTypes_ExternalIntrinsic_IsRejectedByTheStaticDispatcherAlone()
    {
        // The baseline for the test above, and a guard: a later "simplification" back to the static
        // dispatcher must fail here rather than silently discard every non-core descriptor's semantics.
        var instruction = InstructionFor(ExternalIntrinsicName);
        var stack = new List<Type> { typeof(int), typeof(int) };

        var exception = Assert.Throws<InvalidOperationException>(
            () => IntrinsicTypeProcessor.ProcessTypes(instruction, stack));

        Assert.That(exception!.Message, Does.Contain("Unknown intrinsic capability"),
            "the static dispatcher has no entry for a non-core intrinsic; that is the closed universe");
    }

    [Test]
    public void ProcessTypes_DoesNotFallBackToTheStaticDispatcher_WhenTheDescriptorHandlerThrows()
    {
        // A fallback would make the failure harder to find and would give a custom intrinsic the core
        // family's semantics by accident. The descriptor is authoritative for its own name, including when
        // it is wrong.
        var registry = new CilIntrinsicRegistry(
        [
            Descriptor(ExternalIntrinsicName, static (_, _) => throw new InvalidOperationException("handler-ran"))
        ]);

        var exception = Assert.Throws<InvalidOperationException>(
            () => new AbstractMethodsIntrinsicCompiler(registry).ProcessTypes(InstructionFor(ExternalIntrinsicName), []));

        Assert.That(exception!.Message, Is.EqualTo("handler-ran"),
            "the descriptor's failure must surface as itself, not be replaced by a dispatcher error");
    }

    [Test]
    public void ProcessTypes_BuiltInIntrinsics_KeepTheirExistingSemantics()
    {
        // Behaviour preservation. Every default descriptor registers IntrinsicTypeProcessor.ProcessTypes, so
        // honouring the descriptor must change nothing for the shipped instruction set. If this fails, the
        // change altered existing type simulation rather than only opening the extension path.
        var compiler = new AbstractMethodsIntrinsicCompiler(new CilIntrinsicRegistry());

        var cases = new (string Name, Instruction Instruction, List<Type> Initial)[]
        {
            ("load_f64", IntrinsicInstructionFactory.CreateForCapability("load_f64", 1.5d), []),
            ("add_i32", InstructionFor("add_i32"), [typeof(int), typeof(int)]),
            ("boolean_not", InstructionFor("boolean_not"), [typeof(bool)]),
            ("cmp_le_f64", InstructionFor("cmp_le_f64"), [typeof(double), typeof(double)])
        };

        foreach (var (name, instruction, initial) in cases)
        {
            var viaCompiler = initial.ToList();
            var viaSharedProcessor = initial.ToList();

            compiler.ProcessTypes(instruction, viaCompiler);
            IntrinsicTypeProcessor.ProcessTypes(instruction, viaSharedProcessor);

            Assert.That(viaCompiler, Is.EqualTo(viaSharedProcessor),
                $"type simulation for '{name}' must be unchanged by honouring the descriptor");
        }
    }

    [Test]
    public void ProcessTypes_UnregisteredName_StillFailsOnLookup()
    {
        // Registration, not the type stack, decides what exists. An unknown name must fail at the registry
        // lookup with the existing message, so the change does not introduce a second failure vocabulary.
        var exception = Assert.Throws<InvalidOperationException>(
            () => new AbstractMethodsIntrinsicCompiler(new CilIntrinsicRegistry())
                .ProcessTypes(InstructionFor("acme.not_registered"), []));

        Assert.That(exception!.Message, Does.Contain("Unsupported intrinsic"),
            "an unregistered name is an unsupported intrinsic, reported as before");
    }
}
