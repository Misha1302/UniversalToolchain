# Strategy, alternatives, prior art and research positioning

**Status:** conditional 2026-10-08 research synthesis, *not* a marketing claim or a quantitative market study. Source: first ZIP `01_EXECUTIVE_AND_OPTIONS.md`, `03_PRIOR_ART_AND_COMPETITORS.md`, `10_COMPETITIVE_DIMENSIONS.md`; revised ZIP `DECISION_RATIONALE.md`, `DESIGN_ALTERNATIVES.md` and `DECISION_MATRIX_REVISED.csv`.

## Competing strategies (A–H from source, plus product/research variants)

| Strategy | What it optimizes | Main downside | Decision |
| --- | --- | --- | --- |
| A: ideal compiler infrastructure alone | scientific architecture ambition | high investment without author adoption | retain as staged research, not only product |
| B: low-level Language Authoring SDK | already aligned with UT | grammar/binder/tooling still manual | keep stable owner; add narrow ergonomic adapter |
| C: complete stand-alone workbench | integrated IDE language engineering | mature competitors, huge scope | defer |
| D: embeddable DSL platform | .NET user need, deliverable pricing/rules scenarios | less scientifically unique | preferred product wedge |
| E: reusable semantic tooling services | shared meaning across compiler/editor | provenance, trust, cache cost | conditional minimal contract |
| F: AI-native language platform | assistant-assisted authoring | uncontrolled optimism/unsafe authority | optional client only |
| G: integrated Ideal UT | ultimate research/product ambition | massive implementation/verification burden | long-term goal with kill gates |
| H1: contract-native compiler components | research identity | novelty unproven | study with independent package experiment |
| H2: typed embedded rules product | real user adoption | weak differentiation alone | first real external pilot |
| H3: reusable semantic service SDK | cross-layer research/platform | too broad without second consumer | extract only when multiple users need it |

The revised study compares **A0 direct typed interfaces** against evidence-aware H1 and optional typed tooling/agent P1–P5. Its 0–5 cells are expert **ordinal priors with uncertainty intervals**, not experimental scores. There is no defensible numerical overall winner. The reversible experiment is preferred because choice of large architecture flips under plausible rating/weight changes.

## Prior art — strongest relevant rivals, not strawmen

| Research question | Strong rival | UT gap / honest differentiator to test |
| --- | --- | --- |
| Deep IR transformations with typed capabilities | [MLIR](https://mlir.llvm.org/docs/Interfaces/), [ODS](https://mlir.llvm.org/docs/DefiningDialects/Operations/), [PDLL](https://mlir.llvm.org/docs/PDLL/) | MLIR has a mature IR universe; show value for **external different representations**, not just one more op interface |
| Composable syntax and static semantics | [Silver/ableC](https://melt.cs.umn.edu/ableC/), [Statix/Spoofax](https://spoofax.dev/references/statix/) | test independently contributed semantic extensions with explicit backend runtime path |
| Textual DSL authoring and editor | [Langium](https://langium.org/docs/features/), [Xtext](https://eclipse.dev/Xtext/documentation/340_lsp_support.html) | currently UT has worse ready-to-use tooling; compare a narrow complete .NET embedded workflow |
| Structural/projectional tooling | [JetBrains MPS](https://www.jetbrains.com/mps/) | no claim UT invented AI+structure; do not copy mandatory projectional editing |
| Managed compiler/tooling | [Roslyn](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/compiler-api-model) | robust C#/VB tooling; UT targets independent languages/DSLs, not Roslyn replacement |
| Specializing language execution | [Truffle](https://www.graalvm.org/latest/graalvm-as-a-platform/language-implementation-framework/) | measure actual freeze/dispatch/compile cost; no assumed runtime advantage |
| Query/analysis invalidation | [LLVM New PM](https://llvm.org/docs/NewPassManager.html), [Salsa](https://salsa-rs.github.io/salsa/) | exact selected-world evidence revocation across independently developed providers and IR changes |
| Proof/rewrite/inference | [Alive2](https://web.ist.utl.pt/nuno.lopes/pubs.php?id=alive2-mem-cav21), [Soufflé provenance](https://souffle-lang.github.io/provenance), e-graphs/egg | bounded evidence calculus with verified assumptions, not invented proof science |
| Optimizer choice under target properties | Calcite/Cascades | legality and feasibility first; only optimize costs of permissible routes |

## Original 18 comparison dimensions

The source's full eight-competitor matrix covers **(1) simplicity of language creation, (2) modularity, (3) language composability, (4) semantic extensibility, (5) type-system authoring, (6) language evolution, (7) representation neutrality, (8) code generation, (9) runtime, (10) optimization, (11) performance, (12) extensibility cost, (13) IDE tooling, (14) debugging, (15) web integration, (16) AI integration, (17) documentation, (18) ecosystem maturity**. See the **original `10_COMPETITIVE_DIMENSIONS.md`** for its item-by-item judgments and citations; do not compress these unlike-for-like systems into one invented leaderboard.

## Discriminator, not branding

For fixed closed compilers, a good handwritten pipeline is likely simpler and can win on maintainability. For text-first editor UX, Langium/Xtext are strong mature controls; for IR optimization, MLIR wins on established scope. The specific UT hypothesis is **cross-layer composition of independently published semantic evidence and legal transforms** without requiring a universal common IR or pairwise source edits. Test via Range + Shape → unchanged Bounds optimization across actual representations, with an independent semantic oracle and a controlled typed-interface baseline.

## Product/adoption route

Candidate customers: .NET teams embedding pricing/policy DSLs; enterprise policy runtimes needing versioned diagnostics; compiler/analysis researchers; editor tool builders. Validate with **five exploratory design-partner interviews and two real trials**, not guessed ARR. Open-source infrastructure plus paid integration/support is only a hypothesis. A short-term real policy DSL + editor provides user-facing tests and may finance deeper experiments; it does not demonstrate Ideal UT by itself. No revenues, timing, citations of adoption, or performance gains are claimed here.
