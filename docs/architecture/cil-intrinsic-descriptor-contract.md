---
title: CIL intrinsic descriptor contract
navigation: hidden
status: Internal maintainer architecture evidence for the intrinsic descriptor contract (UT-A043, first increment).
description: A registry descriptor owns both halves of an intrinsic; the type-stack half was ignored.
---

# CIL intrinsic descriptor contract

Status: first increment of `UT-A043`. Applies to
`BytecodeDynamicMethodsCompiler.Compilers.CilIntrinsicRegistry` and `AbstractMethodsIntrinsicCompiler`.

## What a descriptor is

`CilIntrinsicDescriptor` carries three things:

| Member | Meaning |
| --- | --- |
| `Name` | the capability id, e.g. `add_i32` or `acme.saturating_add_i32` |
| `Compile` | how to emit the instruction |
| `ProcessTypes` | how the instruction changes the type stack |

Registration is what defines the intrinsic universe. `BuildDefaultDescriptors()` is one particular
registered set, not the definition of what an intrinsic is.

## The defect

Only `Compile` was honoured:

```csharp
public void Compile(CompilationContext context, Instruction instruction, List<Type> stack)
{
    var descriptor = _registry.GetRequired(intrinsic.CapabilityId);
    descriptor.Compile(context, instruction, stack);      // descriptor consulted
}

public void ProcessTypes(Instruction instruction, List<Type> stack)
{
    IntrinsicTypeProcessor.ProcessTypes(instruction, stack);   // descriptor IGNORED
}
```

`IntrinsicTypeProcessor.ProcessTypes` is a static dispatcher over the hardcoded core families
(`call C#`, storage, boolean, arithmetic, comparison, and the `Capability.` namespace). It has no entry
for a name it does not recognise and throws `Unknown intrinsic capability '<id>'`.

So the registry was open and the type stack was closed. An external intrinsic could be registered,
emit correctly, and then be rejected during type simulation — with an error that names the *intrinsic*
as unknown even though it had just been registered. A field that is recorded and never consulted is
worse than an absent one: it reads as a supported contract.

## The rule

**The descriptor is authoritative for its own name.**

`ProcessTypes` now performs the same registry lookup as `Compile` and calls
`descriptor.ProcessTypes`. Two properties follow, and both are tested:

1. **Registration decides existence.** An unregistered name fails at the lookup with the existing
   `Unsupported intrinsic: <name>` message. The change does not introduce a second failure vocabulary.
2. **No fallback.** If a descriptor's handler throws, that exception surfaces as itself. Falling back
   to the static dispatcher would hide the error and could give a custom intrinsic a core family's
   semantics by accident.

For every shipped intrinsic nothing changes, because every default descriptor registers
`IntrinsicTypeProcessor.ProcessTypes` as its handler. That is a deliberate design choice: the default
set keeps one implementation of type simulation while the contract permits another.

## What this increment does not claim

This closes the type-stack half of the extension path only. It does **not** make an external intrinsic
work end to end, and the remaining gaps are real:

- `CilIntrinsicDescriptor` is `internal`, and the descriptor-list constructor is `internal`. A package
  outside this assembly still cannot register anything. The constructor was widened from `private` to
  `internal` so the contract can be tested directly; it is not a public extension point yet.
- There is no package-level registration surface. `LanguagePackageBuilder`, `LanguageFeatureBuilder`,
  `AuthoredLanguagePackage` and `ILanguageExtensionPackage` have no intrinsic hook.
- The interpreter executes only `call C#` and `call C# ctor` and rejects everything else at run time.
  An external intrinsic has no interpreter-side implementation path at all.
- No plan-time check compares a language's required intrinsics against what each enabled backend
  supports, so "fail at plan time" is not achievable today. The earliest failure is the runtime AIR
  verifier (`UT-AIR-INTRINSIC-001`) or the CIL compiler's `Unsupported intrinsic`.
- `IntrinsicDescriptorProviderMetadataValidator`, `IntrinsicSemanticCoverageValidator` and
  `IntrinsicSemanticStartupValidator` are registered in DI but never resolved, so a malformed external
  provider is not detected by any startup check.

`UT-A043`'s own acceptance criterion is "external intrinsic E2E on 2 backends". This increment does not
satisfy it, and should not be presented as if it does.
