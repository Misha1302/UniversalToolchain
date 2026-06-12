# From a composable module to executable CIL

This page maps the talk's "make the abstractions disappear" claim to concrete project paths.

## 1. Language construction remains modular

Arithmetic and variable behavior are owned by feature modules and their AST visitors rather than by a central backend switch.

Representative paths:

- `UniversalToolchain/ArithmeticModule/Visitors/ArithmeticAstVisitor.cs`;
- `UniversalToolchain/NativeMathModule/NativeArithmeticAstVisitor.cs`;
- `UniversalToolchain/VariablesModule/VariablesVisitor.cs`.

The visitors contribute semantic operations to Bytecode. Backend selection has not happened inside these frontend visitors.

## 2. Bytecode is converted into explicit AIR

`UniversalToolchain/AbstractIrConverters/BytecodeToAbstractIrConverterImpl.cs` converts Bytecode operations into Abstract IR and applies stack-type effects while appending the resulting instructions.

This separates two concerns:

- frontend modules contribute language semantics;
- AIR exposes a backend/optimizer-oriented execution representation.

## 3. Specialization is capability-gated

`UniversalToolchain/NativeMathModule/NativeCILOptimizerModule.cs` does not ask whether the backend has a concrete CIL class name. It queries the selected intrinsic capability context.

Only when the selected backend supports the required intrinsic does the optimizer replace a generic sequence with typed operations such as:

- typed constant loads;
- typed external-binding loads.

If the capability is absent, the optimizer preserves the portable representation.

## 4. Locals and external bindings become concrete storage operations

`VariablesVisitor` assigns lexical locals explicit storage keys and external bindings explicit slots.

The CIL intrinsic compiler then lowers storage operations in `UniversalToolchain/BytecodeDynamicMethodsCompiler/Compilers/AbstractMethodsIntrinsicCompiler.cs`:

- a lexical local becomes an IL local through `Ldloc`, `Stloc`, or `Ldloca`;
- an external binding becomes a generated method argument through `Ldarg` or `Starg`;
- the external argument offset accounts for the hidden execution-environment parameter.

The compiled hot path therefore does not repeatedly query a language-module registry to load a value. It executes the storage operation selected during compilation.

## 5. Typed operations are emitted into a DynamicMethod

`UniversalToolchain/BytecodeDynamicMethodsCompiler/Compilers/CilIntrinsicRegistry.cs` maps supported intrinsic symbols to concrete CIL emitters. The generated artifact is a `DynamicMethod`, which is subsequently compiled by the .NET JIT.

The precise claim is:

> The feature-module abstraction is a construction-time mechanism. For supported compiled paths, the selected semantics are lowered into typed CIL operations, leaving no per-operation module dispatch in the prepared artifact's hot invocation path.

This is not a claim that every helper call is guaranteed to be inlined by every JIT version. It is a claim about removing language-construction/plugin dispatch from the generated execution path and presenting typed CIL to the runtime optimizer.

## 6. Why semantic parity is the second half of the design

Specialization is useful only if the representation change preserves meaning. The interpreter remains the semantic reference path, and the CIL path is checked against it through the parity fixtures linked from the [main talk page](README.md).

The binding regression case demonstrates why this matters: if lexical locals and external slots are not explicitly distinguished, a more concrete backend representation can silently redefine the language.
