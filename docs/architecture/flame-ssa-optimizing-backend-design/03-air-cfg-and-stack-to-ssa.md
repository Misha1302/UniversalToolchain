# AIR CFG, verification, stack-to-SSA, types, and storage

## 12. Shared AIR control-flow graph

### 12.1 Why a shared CFG is required

Current AIR is a linear instruction stream with labels and jumps. SSA is graph-based. Correct conversion requires more than iterating instructions and constructing Flame operations.

A shared CFG layer should:

- identify block leaders;
- assign deterministic block identities;
- preserve source/AIR instruction ranges;
- represent explicit jump successors;
- represent conditional successors;
- represent fallthrough successors;
- validate labels;
- compute predecessors;
- preserve unreachable blocks for diagnostics before optional removal;
- support loops and back edges;
- provide stable ordering independent of dictionary enumeration.

### 12.2 Proposed block model

```csharp
public sealed class AirControlFlowGraph
{
    public AirBasicBlock EntryBlock { get; }

    public IReadOnlyList<AirBasicBlock> Blocks { get; }

    public IReadOnlyDictionary<AirBlockId, AirBasicBlock> BlocksById { get; }
}
```

```csharp
public sealed class AirBasicBlock
{
    public AirBlockId Id { get; }

    public IReadOnlyList<Instruction> Body { get; }

    public AirBlockTerminator Terminator { get; }

    public IReadOnlyList<AirControlFlowEdge> Predecessors { get; }

    public IReadOnlyList<AirControlFlowEdge> Successors { get; }
}
```

The graph may be immutable with a dedicated builder. Mutation and analysis caches should not be stored in global static state.

### 12.3 Block formation

Leaders include:

- the first AIR instruction;
- every label;
- the instruction after a terminating jump where such an instruction exists;
- explicit branch targets;
- future exception-region boundaries if exception-aware AIR is introduced.

A block terminator should explicitly distinguish:

- unconditional jump;
- conditional jump with true/false successors;
- fallthrough;
- return/end-of-program;
- unreachable/invalid termination.

AIR currently expresses conditions through stack values and `JmpIf`/`JmpIfNot`; the CFG representation must record which condition is consumed and which successor corresponds to each outcome.

### 12.4 Label validation

The verifier must diagnose:

- duplicate labels;
- jumps to unknown labels;
- malformed label identifiers;
- labels with conflicting ownership metadata if such metadata is present;
- invalid fallthrough after unconditional termination;
- impossible block entry assumptions.

Diagnostics must be deterministic and source-correlated where annotations permit it.

## 13. Typed stack data-flow analysis

### 13.1 Required behavior

For each basic block, the analysis must determine the typed evaluation stack at entry and exit.

The algorithm is a forward fixed-point analysis:

1. Initialize the entry block with the compilation entry stack contract.
2. Simulate every instruction in a reachable block.
3. Compute the outgoing stack for each successor edge.
4. Merge the outgoing stack into the successor's input state.
5. If the successor input changes, enqueue the successor again.
6. Continue until no state changes or an error prevents meaningful continuation.

### 13.2 Merge rules

At a control-flow join:

- incoming stack depths must match;
- each stack position must have compatible types;
- value categories such as managed reference, byref, pointer, numeric, and unknown must merge under an explicit rule;
- incompatible states are verification errors;
- unknown type must not silently absorb a known incompatible type unless the AIR contract explicitly defines such behavior;
- merge order must not affect the result.

Example error:

```text
AIR verification failed at block 'discount.merge'.

Incoming stack from 'discount.full':
  [System.Decimal, System.Boolean]

Incoming stack from 'discount.capped':
  [System.Decimal]

All predecessor stacks must have the same depth.
```

### 13.3 Loop handling

Loops require fixed-point iteration. The implementation must not assume a block is analyzed only once.

The analysis should include:

- worklist ordering that is deterministic;
- change detection based on immutable stack-state values;
- a bounded diagnostic strategy for malformed graphs;
- no arbitrary iteration cutoff for valid monotone type propagation;
- explicit handling of irreducible CFGs if they can be produced.

### 13.4 Shared ownership

The analysis belongs in a generic AIR analysis package. Flame lowering consumes its result. The current CIL verifier/simulator should migrate toward the same source of truth where practical, avoiding two independent stack semantics.

## 14. Stack-to-SSA lowering

### 14.1 Core transformation

Every AIR stack slot at a block entry becomes a Flame block parameter.

Every outgoing edge passes the current SSA value for each stack slot as branch arguments.

Example AIR:

```text
Push condition
JmpIf use_high
Push 10
Jmp merge

use_high:
Push 20

merge:
Push 2
Intrinsic multiply
```

Conceptual SSA:

```text
entry:
  condition = ...
  branch condition ? use_high : use_low

use_low:
  ten = const 10
  jump merge(ten)

use_high:
  twenty = const 20
  jump merge(twenty)

merge(value):
  two = const 2
  result = multiply(value, two)
  return result
```

### 14.2 Lowering state

For every block, lowering needs:

- the mapped Flame block builder;
- block parameter tags matching the verified input stack;
- a mutable local stack of Flame value tags while lowering the block body;
- source/AIR annotation mapping;
- a stable name allocator used only for diagnostics and IR display;
- access to type, intrinsic, managed call, and constant lowering services.

### 14.3 Determinism

Deterministic output requires:

- stable block traversal order;
- stable predecessor ordering;
- stable block and value naming;
- stable diagnostics sorting;
- no dependence on service registration order except where explicitly ordered by contract;
- no random GUID-based display names in serialized IR;
- stable optimization profile ordering.

Internal object identities may be non-deterministic, but serialized reports and persisted artifacts should not expose accidental process order.

### 14.4 Drop and unused values

AIR `Drop` removes the top stack value. In SSA this generally means the value has no uses. Dead-value elimination may later remove its producer only when doing so is legal under the effect model.

A dropped side-effecting call must still execute.

### 14.5 Annotations

AIR annotations should be preserved into a backend-neutral source map or report metadata where possible. Flame IR should not become the only storage location for diagnostics because that would leak backend types into generic reporting.

## 15. Type system mapping

### 15.1 Type ownership

UniversalToolchain source and binding types remain expressed through current framework contracts and CLR `Type` information. Flame's `IType` is a backend-local representation.

### 15.2 Initial supported types

The first implementation should support a narrow, explicit set:

- `System.Boolean`;
- signed and unsigned integer primitives required by current Wist paths;
- `System.Single`;
- `System.Double`;
- `System.Decimal` through managed calls or supported lowering;
- `System.String` where existing semantics allow it;
- external reference types used as statically typed managed call receivers;
- `void`/no-result semantics;
- nullable/reference values only after exact behavior is specified.

### 15.3 Decimal

`System.Decimal` is not a primitive CIL arithmetic type. Existing semantics commonly lower decimal operations to managed methods/operators. The Flame backend should preserve the same behavior unless it has a proven equivalent representation.

### 15.4 Reference and byref types

Flame has explicit pointer kinds for box/reference/transient pointer semantics. Mapping must be conservative. `ref`, `out`, managed references, and address-taking should not be enabled merely because Flame can represent pointers.

Each byref operation requires:

- lifetime rules;
- escape rules;
- aliasing assumptions;
- compatibility with generated CIL verification;
- interpreter parity or an explicit unsupported-backend diagnostic.

### 15.5 Generic types and methods

Closed generic types and methods can be supported when exact CLR members are already resolved. Open generic inference belongs to the semantic resolver, not the Flame backend.

## 16. Local variables and storage

### 16.1 Current state

Current local variables are lowered through execution-scoped managed runtime calls. This preserves interpreter universality and avoids global mutable storage.

### 16.2 Initial Flame support

The first Flame prototype may lower those existing runtime calls as ordinary managed calls. This enables correctness before storage optimization.

The consequence is that some local-variable optimizations remain opaque to Flame.

### 16.3 Target storage semantics

A later shared AIR evolution may introduce explicit backend-neutral storage operations:

```text
storage.allocate
storage.load
storage.store
storage.address
```

Backend mappings:

```text
interpreter:
  execution-scoped runtime storage

cil:
  CIL locals or current optimized lowering

optimized-cil:
  Flame alloca/load/store
  followed by legal register promotion or scalar replacement
```

This must not reintroduce static/global `VariablesContainer<T>` state.

### 16.4 Storage effects

Loads and stores require memory effect descriptors. Optimizers must understand aliasing conservatively. A local store cannot be reordered around an unknown managed call if that call may access the same storage or external state.


[Back to the design dossier index](index.md)
