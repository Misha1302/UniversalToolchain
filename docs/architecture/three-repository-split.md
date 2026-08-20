# Three-repository architecture

The canonical source architecture is partitioned into three owners:

- **UNIVERSAL** — language-neutral UniversalToolchain framework and generic fixtures;
- **WIST_PRODUCT** — Wist language, product tooling, examples, benchmarks and tests;
- **PLANFUZZ_RESEARCH** — PlanFuzz research, replay and adapters.

Canonical monorepo solutions are checked in and static:

```text
UniversalToolchain/UniversalToolchain.sln
UniversalToolchain/Wist.sln
UniversalToolchain/PlanFuzz.sln
```

Solution membership is not ownership. `eng/project-ownership.json` is the source-project ownership contract and `eng/repository-partitions.json` owns non-project files. The architecture validator checks both ownership and actual dependency edges.

Component builds run architecture checks before restore:

```bash
./build.sh --component universal
./build.sh --component wist
./build.sh --component planfuzz
./build.sh --all
```

For deterministic diagnosis use `--serial --no-build-servers`.

The physical repository candidates use package boundaries: Wist consumes a reviewed UniversalToolchain artifact feed and PlanFuzz consumes reviewed UniversalToolchain/Wist artifacts. Cross-repository `ProjectReference`, IVT, source include, import and path-probing shortcuts are forbidden.
