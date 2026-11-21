using System.Text;
using BasicCore.TranslatorWrapper;

namespace UniversalToolchain.LanguageSdk.Tests;

/// <summary>
/// Stable textual form of a <see cref="Bytecode"/> value.
/// <para>
/// <see cref="Bytecode.ToString"/> cannot be used as a golden value: instruction tags live in a
/// <see cref="HashSet{T}"/>, so their order is not guaranteed to be reproducible across runtimes or
/// even across processes. A golden file written from that output would be flaky in a way that looks
/// like a lowering change, which is exactly the false signal this corpus exists to prevent.
/// </para>
/// <para>
/// This renderer sorts tags ordinally and keeps instruction order and operand text intact, because
/// instruction order <em>is</em> the behaviour being pinned.
/// </para>
/// </summary>
internal static class CanonicalBytecodeText
{
    public static string Render(Bytecode bytecode)
    {
        ArgumentNullException.ThrowIfNull(bytecode);
        var builder = new StringBuilder();
        foreach (var instruction in bytecode.Instructions)
        {
            var tags = instruction.Tags
                .OrderBy(static tag => tag, StringComparer.Ordinal);
            builder.Append('[').AppendJoin(", ", tags).Append("] ");

            var levels = instruction.Ops
                .OrderBy(static level => level.Key)
                .Select(static level => string.Join(", ", level.Value.Select(static op => op.ToString())));
            builder.AppendJoin(" | ", levels);
            builder.Append('\n');
        }

        return builder.ToString();
    }
}
