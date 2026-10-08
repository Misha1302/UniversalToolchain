# LangDev 2026 presentation alignment

Sources: [production deck](https://github.com/Misha1302/lang-dev-presentation-2026/blob/main/index.html), [README](https://github.com/Misha1302/lang-dev-presentation-2026/blob/main/README.md), [rebuild evidence](https://github.com/Misha1302/lang-dev-presentation-2026/blob/main/REBUILD_EVIDENCE.md), [QA](https://github.com/Misha1302/lang-dev-presentation-2026/blob/main/QA_REPORT.md), and [deployed address](https://misha1302.github.io/lang-dev-presentation-2026/). Deck contains **18 slides**, 21:30 rehearsal allocation, **6-minute proposed research section**. Compiler claims pin `UniversalToolchain@1d46f17`. Public site URL was not reachable via the available web reader; actual source and QA notes were read through GitHub.

| Slides | Meaning | State |
| --- | --- | --- |
| 1–4 | Wist feature contributes across stages; independent modules form a restricted pricing DSL | CURRENT, example backed by repository |
| 5 | Packages/definition resolve through sole `LanguageCompiler` to immutable `LanguagePlan` | CURRENT, plan is selection data |
| 6–7 | Wist semantic binding, Bytecode/AIR, interpreter and CIL; exact external-load pattern lowered via capability to typed CIL load | CURRENT, bounded implementation/test evidence |
| 8 | External/local shadowing and binding storage must agree across interpreter/CIL | CURRENT, regression and negative example; do not invent prior wrong output |
| 9–15 | Typed semantic evidence, proof DAG, new provider contributing missing premises to **unchanged** optimizer rule, provenance/invalidation, Unknown behavior and conceptual SIMD | PROPOSED, not general UT proof API or production SIMD |
| 16–18 | Return to thesis, compare prior art, acknowledge limitations | POSITIONING, not measured novelty |

The SIMD proof-DAG demonstrates a conditional proposition: rule legality requires appropriate type/lane, bounds, alias/effects, traps/tail and target; missing one ⇒ Unknown ⇒ conservative scalar path. A new provider can supply a premise only under exact trust/revision scope; it does not alter the rule, establish arbitrary provider correctness, guarantee profit or prove the vectorizer exists. Distinguish semantic legality from profitability.

### Claim rules

- Extensibility machinery does not vanish *globally*; it is resolved before selected execution.
- `LanguagePlan` structural compatibility ≠ behavioral compatibility, package safety or semantic proof.
- Zero allocations in a **selected measured hot scenario** ≠ universal "zero-cost abstraction."
- SIMD, semantic fact exchange and rule proofs must always be labeled **PROPOSAL/RESEARCH**, while Wist CIL/parity witnesses are current.
- The strongest criticism remains: a fixed handwritten pipeline with typed interfaces may suffice. Ideal UT must demonstrate a controlled improvement.
