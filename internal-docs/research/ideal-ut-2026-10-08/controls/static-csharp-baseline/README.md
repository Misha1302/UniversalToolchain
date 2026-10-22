# Minimal typed-C# composition control — research only

**Status:** locally executed on 2026-10-23 with .NET SDK `10.0.301`, Linux x64; 12 decision assertions passed. **Not** UniversalToolchain integration, production implementation, formal verification, or machine-code bounds-check elimination.

This is the exact five-project source control used in [EXPERIMENT_DESIGN.md](../../EXPERIMENT_DESIGN.md), versioned here so later reviewers do not need a chat attachment for reproduction.

```sh
dotnet --version
dotnet run --project Host/Host.csproj -c Release --ignore-failed-sources
sha256sum -c SOURCE_SHA256.txt
```

Expected result: `PASS: 12 decision assertions; no UT runtime, LLVM, MLIR or machine-code optimizer invoked.` See the full observed output in [EXPERIMENT_DESIGN.md](../../EXPERIMENT_DESIGN.md).

Five independently compiled project identities:
- `Shared`: pre-agreed contracts and `SimpleSafetyQuery` glue.
- `Shape`, `Range`: two independent data producers and higher precision/conflicting substitutes.
- `Consumer`: unchanged `BoundsOptimization`, depends **only** on Shared.
- `Host`: explicit composition root and twelve decision checks.

The baseline Range cannot prove the safe index-zero case while the precise Range can, so the revised control now tests a genuine precision difference rather than only provider substitution.

No consumer or prior provider source edits are needed when the host adds another provider **because** consumer already calls a shared `ISafeIndexQuery`. Introducing a wholly new semantic obligation still needs a hook or new transform. The `ImplementationHash` is a **caller-claimed string**, not an authenticated assembly digest. The `LengthStable` and `AliasingSafe` booleans are **assumptions**, not independent semantic proofs. The test checks **a boolean decision** and does not produce, execute or validate an optimized `Read(a,i)`. Never quote the 10/10 result as correctness of any real bounds-check optimization.

Source SHA-256 values are recorded in `SOURCE_SHA256.txt` for exact repeatability. The original source archive in the research task was independently hashed as `94996d261cb8fab9db7d8d0445bee37f1d6d75603db5e930cbb1a06157e34903`; this source directory is the expanded equivalent (excluding build output and original captured log).

**Stop criterion:** if the full E1 alternative obtains equivalent safe behavior with fewer concepts, do not generalize into a new semantic core. Primary question IDs: Q07, Q09, Q10, Q12, Q15 and Q42.
