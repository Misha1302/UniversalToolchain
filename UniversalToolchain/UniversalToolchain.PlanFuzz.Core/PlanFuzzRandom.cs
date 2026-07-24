namespace UniversalToolchain.PlanFuzz;

/// <summary>
/// Provides a versioned deterministic xoshiro256** sequence with stable domain-separated forks.
/// </summary>
public sealed class PlanFuzzRandom
{
    public const string AlgorithmId = "xoshiro256starstar-v1";

    private readonly ulong _rootSeed;
    private ulong _state0;
    private ulong _state1;
    private ulong _state2;
    private ulong _state3;

    public PlanFuzzRandom(ulong seed)
    {
        _rootSeed = seed;
        var splitMixState = seed;
        _state0 = NextSplitMix64(ref splitMixState);
        _state1 = NextSplitMix64(ref splitMixState);
        _state2 = NextSplitMix64(ref splitMixState);
        _state3 = NextSplitMix64(ref splitMixState);

        if ((_state0 | _state1 | _state2 | _state3) == 0)
            _state0 = 0x9E3779B97F4A7C15UL;
    }

    public ulong NextUInt64()
    {
        var result = RotateLeft(_state1 * 5, 7) * 9;
        var temporary = _state1 << 17;

        _state2 ^= _state0;
        _state3 ^= _state1;
        _state1 ^= _state2;
        _state0 ^= _state3;
        _state2 ^= temporary;
        _state3 = RotateLeft(_state3, 45);

        return result;
    }

    public int NextInt32(int exclusiveMax)
    {
        if (exclusiveMax <= 0)
            return Thrower.Argument<int>(nameof(exclusiveMax), "Exclusive maximum must be positive.");

        var bound = (uint)exclusiveMax;
        var threshold = unchecked((uint)(0 - bound)) % bound;
        while (true)
        {
            var value = (uint)NextUInt64();
            if (value >= threshold)
                return (int)(value % bound);
        }
    }

    public bool NextBoolean() => (NextUInt64() & 1UL) != 0;

    public PlanFuzzRandom Fork(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
            return Thrower.Argument<PlanFuzzRandom>(nameof(domain), "Fork domain must not be empty.");

        var domainBytes = Encoding.UTF8.GetBytes(domain);
        var algorithmBytes = Encoding.UTF8.GetBytes(AlgorithmId);
        var input = new byte[sizeof(ulong) + sizeof(int) + algorithmBytes.Length + domainBytes.Length];
        BinaryPrimitives.WriteUInt64LittleEndian(input.AsSpan(0, sizeof(ulong)), _rootSeed);
        BinaryPrimitives.WriteInt32LittleEndian(input.AsSpan(sizeof(ulong), sizeof(int)), algorithmBytes.Length);
        algorithmBytes.CopyTo(input.AsSpan(sizeof(ulong) + sizeof(int)));
        domainBytes.CopyTo(input.AsSpan(sizeof(ulong) + sizeof(int) + algorithmBytes.Length));
        var hash = SHA256.HashData(input);
        return new PlanFuzzRandom(BinaryPrimitives.ReadUInt64LittleEndian(hash));
    }

    private static ulong NextSplitMix64(ref ulong state)
    {
        state += 0x9E3779B97F4A7C15UL;
        var value = state;
        value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
        value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
        return value ^ (value >> 31);
    }

    private static ulong RotateLeft(ulong value, int count) =>
        (value << count) | (value >> (64 - count));
}
