---
layout: home

hero:
  name: UniversalToolchain
  text: Build languages, not one-off parsers.
  tagline: A developer documentation hub for Wist, DSL composition, language modules, bytecode, AIR, and execution backends.
  image:
    src: /hero.svg
    alt: UniversalToolchain pipeline
  actions:
    - theme: brand
      text: Start with Wist
      link: /start/
    - theme: alt
      text: Build a DSL
      link: /build-dsls/
    - theme: alt
      text: Explore Internals
      link: /internals/

features:
  - icon: 🚀
    title: Fast entry
    details: Start by running Wist, then gradually move into dialects, modules, and internals.
  - icon: 🧩
    title: Modular language design
    details: Treat language features as composable modules instead of hardcoded compiler branches.
  - icon: ⚙️
    title: Runtime-aware architecture
    details: Follow the path from source code to bytecode, AIR, optimizers, and execution backends.
---

## Choose your path

<div class="doc-paths">
  <div class="doc-path-card">
    <h3>I want to use Wist</h3>
    <p>Run the reference language, learn the syntax, and understand the basic development flow.</p>
    <a href="/start/">Start here →</a>
  </div>

  <div class="doc-path-card">
    <h3>I want to build a DSL</h3>
    <p>Compose existing language features into a small domain-specific language using dialect files.</p>
    <a href="/build-dsls/">Build your first DSL →</a>
  </div>

  <div class="doc-path-card">
    <h3>I want to write a module</h3>
    <p>Add a language feature: syntax recognition, AST nodes, bytecode generation, and tests.</p>
    <a href="/write-modules/">Write a module →</a>
  </div>

  <div class="doc-path-card">
    <h3>I want to understand the runtime</h3>
    <p>Go deeper into bytecode, semantic tags, AIR, optimizations, interpreter execution, and CIL generation.</p>
    <a href="/internals/">Explore internals →</a>
  </div>
</div>

## Mental model

UniversalToolchain is not just a language. It is a framework for building languages.

<div class="pipeline-strip">
  <span class="pipeline-step">Source</span>
  <span class="pipeline-step">Lexer</span>
  <span class="pipeline-step">Parser</span>
  <span class="pipeline-step">AST</span>
  <span class="pipeline-step">Bytecode</span>
  <span class="pipeline-step">AIR</span>
  <span class="pipeline-step">Optimizers</span>
  <span class="pipeline-step">Interpreter / CIL</span>
</div>

<div class="next-box">

### Recommended reading order

1. Start with Wist.
2. Learn the mental model.
3. Build a tiny DSL from existing modules.
4. Write one language module.
5. Only then go into bytecode, AIR, optimizers, and backend contracts.

</div>
