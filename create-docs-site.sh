#!/usr/bin/env bash
set -euo pipefail

# Creates a VitePress documentation site for UniversalToolchain/Wist.
#
# Usage:
#   bash create-docs-site.sh
#
# Optional:
#   FORCE=1 bash create-docs-site.sh
#   BASE_PATH=/Wist2/ bash create-docs-site.sh
#   PROJECT_TITLE="UniversalToolchain" bash create-docs-site.sh
#   GITHUB_URL="https://github.com/Misha1302/Wist2" bash create-docs-site.sh
#   INSTALL=1 bash create-docs-site.sh

FORCE="${FORCE:-0}"
INSTALL="${INSTALL:-0}"
PROJECT_TITLE="${PROJECT_TITLE:-UniversalToolchain}"

log() {
    printf '\033[1;32m%s\033[0m\n' "$1"
}

warn() {
    printf '\033[1;33m%s\033[0m\n' "$1"
}

write_file() {
    local path="$1"
    mkdir -p "$(dirname "$path")"

    if [[ -f "$path" && "$FORCE" != "1" ]]; then
        warn "skip: $path already exists. Use FORCE=1 to overwrite."
        cat >/dev/null
        return 0
    fi

    cat > "$path"
    log "write: $path"
}

infer_repo_name() {
    local url
    url="$(git config --get remote.origin.url 2>/dev/null || true)"

    if [[ -z "$url" ]]; then
        printf "docs"
        return 0
    fi

    url="${url%.git}"
    url="${url##*/}"
    printf "%s" "$url"
}

infer_github_url() {
    local url
    url="$(git config --get remote.origin.url 2>/dev/null || true)"
    url="${url%.git}"

    if [[ "$url" == git@github.com:* ]]; then
        url="${url#git@github.com:}"
        printf "https://github.com/%s" "$url"
        return 0
    fi

    if [[ "$url" == https://github.com/* ]]; then
        printf "%s" "$url"
        return 0
    fi

    printf "https://github.com/Misha1302/Wist2"
}

infer_base_path() {
    local repo
    repo="$(infer_repo_name)"

    if [[ "$repo" == *.github.io ]]; then
        printf "/"
    else
        printf "/%s/" "$repo"
    fi
}

escape_ts_string() {
    printf "%s" "$1" | sed "s/\\\\/\\\\\\\\/g; s/'/\\\\'/g"
}

REPO_NAME="$(infer_repo_name)"
BASE_PATH="${BASE_PATH:-$(infer_base_path)}"
GITHUB_URL="${GITHUB_URL:-$(infer_github_url)}"
DEFAULT_BRANCH="$(git rev-parse --abbrev-ref HEAD 2>/dev/null || printf "master")"

TITLE_TS="$(escape_ts_string "$PROJECT_TITLE")"
GITHUB_URL_TS="$(escape_ts_string "$GITHUB_URL")"
BASE_PATH_TS="$(escape_ts_string "$BASE_PATH")"
DEFAULT_BRANCH_TS="$(escape_ts_string "$DEFAULT_BRANCH")"

log "Project title: $PROJECT_TITLE"
log "GitHub URL:    $GITHUB_URL"
log "Base path:     $BASE_PATH"
log "Branch:        $DEFAULT_BRANCH"

# --------------------------------------------------------------------
# package.json
# --------------------------------------------------------------------

if [[ -f "package.json" ]]; then
    if command -v node >/dev/null 2>&1; then
        node <<'NODE'
const fs = require('fs');

const path = 'package.json';
const pkg = JSON.parse(fs.readFileSync(path, 'utf8'));

pkg.private = pkg.private ?? true;
pkg.scripts = pkg.scripts || {};
pkg.scripts['docs:dev'] = pkg.scripts['docs:dev'] || 'vitepress dev docs --host 0.0.0.0';
pkg.scripts['docs:build'] = pkg.scripts['docs:build'] || 'vitepress build docs';
pkg.scripts['docs:preview'] = pkg.scripts['docs:preview'] || 'vitepress preview docs --host 0.0.0.0';

pkg.devDependencies = pkg.devDependencies || {};
pkg.devDependencies.vitepress = pkg.devDependencies.vitepress || 'latest';

fs.writeFileSync(path, JSON.stringify(pkg, null, 2) + '\n');
NODE
        log "update: package.json"
    else
        warn "package.json exists, but node is not available. Add VitePress scripts manually."
    fi
else
    write_file "package.json" <<'EOF'
{
  "private": true,
  "scripts": {
    "docs:dev": "vitepress dev docs --host 0.0.0.0",
    "docs:build": "vitepress build docs",
    "docs:preview": "vitepress preview docs --host 0.0.0.0"
  },
  "devDependencies": {
    "vitepress": "latest"
  }
}
EOF
fi

# --------------------------------------------------------------------
# VitePress config
# --------------------------------------------------------------------

write_file "docs/.vitepress/config.mts" <<EOF
import { defineConfig } from 'vitepress'

export default defineConfig({
    title: '${TITLE_TS}',
    description: 'Developer documentation for UniversalToolchain and Wist.',
    base: '${BASE_PATH_TS}',
    cleanUrls: true,
    lastUpdated: true,

    head: [
        ['meta', { name: 'theme-color', content: '#0f172a' }],
        ['link', { rel: 'icon', type: 'image/svg+xml', href: '${BASE_PATH_TS}logo.svg' }]
    ],

    themeConfig: {
        logo: '/logo.svg',

        nav: [
            { text: 'Start', link: '/start/' },
            { text: 'Wist', link: '/wist/' },
            { text: 'Build DSLs', link: '/build-dsls/' },
            { text: 'Write Modules', link: '/write-modules/' },
            { text: 'Internals', link: '/internals/' },
            { text: 'Reference', link: '/reference/' },
            { text: 'GitHub', link: '${GITHUB_URL_TS}' }
        ],

        search: {
            provider: 'local'
        },

        outline: {
            level: [2, 3],
            label: 'On this page'
        },

        socialLinks: [
            { icon: 'github', link: '${GITHUB_URL_TS}' }
        ],

        editLink: {
            pattern: '${GITHUB_URL_TS}/edit/${DEFAULT_BRANCH_TS}/docs/:path',
            text: 'Edit this page on GitHub'
        },

        footer: {
            message: 'Built for developers who want to use, extend, or understand UniversalToolchain.',
            copyright: 'UniversalToolchain documentation'
        },

        sidebar: {
            '/start/': [
                {
                    text: 'Start Here',
                    items: [
                        { text: 'Overview', link: '/start/' },
                        { text: 'What is UniversalToolchain?', link: '/start/what-is-universal-toolchain' },
                        { text: 'What is Wist?', link: '/start/what-is-wist' },
                        { text: 'Installation', link: '/start/installation' },
                        { text: 'First Program', link: '/start/first-program' },
                        { text: 'Mental Model', link: '/start/mental-model' }
                    ]
                }
            ],

            '/wist/': [
                {
                    text: 'Wist Language',
                    items: [
                        { text: 'Overview', link: '/wist/' },
                        { text: 'Syntax Tour', link: '/wist/syntax-tour' },
                        { text: 'Numbers', link: '/wist/numbers' },
                        { text: 'Variables', link: '/wist/variables' },
                        { text: 'Conditions', link: '/wist/conditions' },
                        { text: 'Loops', link: '/wist/loops' },
                        { text: 'Scopes', link: '/wist/scopes' },
                        { text: 'Examples', link: '/wist/examples' }
                    ]
                }
            ],

            '/build-dsls/': [
                {
                    text: 'Build DSLs',
                    items: [
                        { text: 'Overview', link: '/build-dsls/' },
                        { text: 'Dialect Files', link: '/build-dsls/dialect-files' },
                        { text: 'Module Composition', link: '/build-dsls/module-composition' },
                        { text: 'Minimal DSL', link: '/build-dsls/minimal-dsl' },
                        { text: 'Backend Selection', link: '/build-dsls/backend-selection' },
                        { text: 'Testing a DSL', link: '/build-dsls/testing-dsl' }
                    ]
                }
            ],

            '/write-modules/': [
                {
                    text: 'Write Modules',
                    items: [
                        { text: 'Overview', link: '/write-modules/' },
                        { text: 'Frontend Module', link: '/write-modules/frontend-module' },
                        { text: 'Parser Extension', link: '/write-modules/parser-extension' },
                        { text: 'AST Nodes', link: '/write-modules/ast-nodes' },
                        { text: 'Bytecode Generation', link: '/write-modules/bytecode-generation' },
                        { text: 'Semantic Tags', link: '/write-modules/semantic-tags' },
                        { text: 'Ordering and Priority', link: '/write-modules/ordering-and-priority' },
                        { text: 'Testing a Module', link: '/write-modules/testing-module' }
                    ]
                }
            ],

            '/internals/': [
                {
                    text: 'Internals',
                    items: [
                        { text: 'Overview', link: '/internals/' },
                        { text: 'Pipeline', link: '/internals/pipeline' },
                        { text: 'Lexer', link: '/internals/lexer' },
                        { text: 'Parser', link: '/internals/parser' },
                        { text: 'AST', link: '/internals/ast' },
                        { text: 'Bytecode', link: '/internals/bytecode' },
                        { text: 'AIR', link: '/internals/air' },
                        { text: 'Backends', link: '/internals/backends' },
                        { text: 'Intrinsics', link: '/internals/intrinsics' },
                        { text: 'Optimizers', link: '/internals/optimizers' },
                        { text: 'Semantic Parity', link: '/internals/semantic-parity' },
                        { text: 'Dependency Injection', link: '/internals/dependency-injection' }
                    ]
                }
            ],

            '/reference/': [
                {
                    text: 'Reference',
                    items: [
                        { text: 'Overview', link: '/reference/' },
                        { text: 'Dialect Reference', link: '/reference/dialect-reference' },
                        { text: 'Module Reference', link: '/reference/module-reference' },
                        { text: 'Bytecode Reference', link: '/reference/bytecode-reference' },
                        { text: 'AIR Reference', link: '/reference/air-reference' },
                        { text: 'Intrinsics Reference', link: '/reference/intrinsics-reference' },
                        { text: 'Backend Contracts', link: '/reference/backend-contracts' },
                        { text: 'Project Rules', link: '/reference/project-rules' }
                    ]
                }
            ]
        }
    }
})
EOF

write_file "docs/.vitepress/theme/index.ts" <<'EOF'
import DefaultTheme from 'vitepress/theme'
import './style.css'

export default DefaultTheme
EOF

write_file "docs/.vitepress/theme/style.css" <<'EOF'
:root {
    --vp-c-brand-1: #7c3aed;
    --vp-c-brand-2: #6d28d9;
    --vp-c-brand-3: #8b5cf6;

    --vp-c-bg: #ffffff;
    --vp-c-bg-alt: #f8fafc;
    --vp-c-bg-soft: #f1f5f9;

    --vp-home-hero-name-color: transparent;
    --vp-home-hero-name-background: linear-gradient(120deg, #7c3aed 0%, #2563eb 45%, #0891b2 100%);

    --vp-home-hero-image-background-image: linear-gradient(135deg, rgba(124, 58, 237, 0.28), rgba(37, 99, 235, 0.18), rgba(8, 145, 178, 0.20));
    --vp-home-hero-image-filter: blur(44px);
}

.dark {
    --vp-c-bg: #020617;
    --vp-c-bg-alt: #0f172a;
    --vp-c-bg-soft: #111827;

    --vp-c-brand-1: #a78bfa;
    --vp-c-brand-2: #8b5cf6;
    --vp-c-brand-3: #7c3aed;
}

.VPHome .VPHomeHero .name,
.VPHome .VPHomeHero .text {
    letter-spacing: -0.04em;
}

.VPHome .VPHomeHero .tagline {
    max-width: 720px;
    font-size: 20px;
    line-height: 1.55;
}

.VPFeature {
    border: 1px solid rgba(124, 58, 237, 0.14);
    background: linear-gradient(180deg, rgba(124, 58, 237, 0.045), rgba(37, 99, 235, 0.025));
}

.dark .VPFeature {
    border-color: rgba(167, 139, 250, 0.18);
    background: linear-gradient(180deg, rgba(124, 58, 237, 0.12), rgba(14, 165, 233, 0.06));
}

.vp-doc .doc-paths {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 18px;
    margin-top: 28px;
}

.vp-doc .doc-path-card {
    position: relative;
    overflow: hidden;
    border: 1px solid var(--vp-c-divider);
    border-radius: 20px;
    padding: 22px;
    background:
        radial-gradient(circle at top right, rgba(124, 58, 237, 0.12), transparent 34%),
        var(--vp-c-bg-soft);
    transition: transform 0.18s ease, border-color 0.18s ease, box-shadow 0.18s ease;
}

.vp-doc .doc-path-card:hover {
    transform: translateY(-2px);
    border-color: var(--vp-c-brand-2);
    box-shadow: 0 16px 40px rgba(15, 23, 42, 0.10);
}

.dark .vp-doc .doc-path-card:hover {
    box-shadow: 0 16px 40px rgba(0, 0, 0, 0.28);
}

.vp-doc .doc-path-card h3 {
    margin-top: 0;
    margin-bottom: 8px;
    font-size: 20px;
}

.vp-doc .doc-path-card p {
    margin: 0 0 16px;
    color: var(--vp-c-text-2);
}

.vp-doc .doc-path-card a {
    font-weight: 650;
    text-decoration: none;
}

.vp-doc .pipeline-strip {
    display: flex;
    flex-wrap: wrap;
    gap: 10px;
    margin: 28px 0;
}

.vp-doc .pipeline-step {
    border: 1px solid var(--vp-c-divider);
    border-radius: 999px;
    padding: 8px 13px;
    background: var(--vp-c-bg-soft);
    color: var(--vp-c-text-1);
    font-size: 14px;
    font-weight: 650;
}

.vp-doc .next-box {
    margin-top: 32px;
    border: 1px solid rgba(124, 58, 237, 0.20);
    border-radius: 20px;
    padding: 20px;
    background: linear-gradient(135deg, rgba(124, 58, 237, 0.10), rgba(14, 165, 233, 0.06));
}

.vp-doc .placeholder-box {
    border: 1px dashed var(--vp-c-divider);
    border-radius: 18px;
    padding: 18px;
    background: var(--vp-c-bg-soft);
}

@media (max-width: 760px) {
    .vp-doc .doc-paths {
        grid-template-columns: 1fr;
    }

    .VPHome .VPHomeHero .tagline {
        font-size: 17px;
    }
}
EOF

# --------------------------------------------------------------------
# Public assets
# --------------------------------------------------------------------

write_file "docs/public/logo.svg" <<'EOF'
<svg width="96" height="96" viewBox="0 0 96 96" fill="none" xmlns="http://www.w3.org/2000/svg">
  <rect width="96" height="96" rx="24" fill="#0F172A"/>
  <path d="M24 29h48v9H54v34H42V38H24v-9Z" fill="url(#g1)"/>
  <path d="M27 65h42v8H27v-8Z" fill="url(#g2)" opacity="0.95"/>
  <path d="M27 49h13v8H27v-8Zm29 0h13v8H56v-8Z" fill="#38BDF8" opacity="0.9"/>
  <defs>
    <linearGradient id="g1" x1="24" y1="29" x2="72" y2="72" gradientUnits="userSpaceOnUse">
      <stop stop-color="#A78BFA"/>
      <stop offset="0.5" stop-color="#60A5FA"/>
      <stop offset="1" stop-color="#22D3EE"/>
    </linearGradient>
    <linearGradient id="g2" x1="27" y1="65" x2="69" y2="73" gradientUnits="userSpaceOnUse">
      <stop stop-color="#7C3AED"/>
      <stop offset="1" stop-color="#06B6D4"/>
    </linearGradient>
  </defs>
</svg>
EOF

write_file "docs/public/hero.svg" <<'EOF'
<svg width="560" height="420" viewBox="0 0 560 420" fill="none" xmlns="http://www.w3.org/2000/svg">
  <rect x="76" y="56" width="408" height="308" rx="32" fill="#0F172A"/>
  <rect x="104" y="90" width="352" height="48" rx="14" fill="#1E293B"/>
  <circle cx="126" cy="114" r="6" fill="#F87171"/>
  <circle cx="146" cy="114" r="6" fill="#FBBF24"/>
  <circle cx="166" cy="114" r="6" fill="#34D399"/>
  <rect x="104" y="162" width="98" height="46" rx="14" fill="#312E81"/>
  <rect x="232" y="162" width="96" height="46" rx="14" fill="#1D4ED8"/>
  <rect x="358" y="162" width="98" height="46" rx="14" fill="#0E7490"/>
  <path d="M202 185h30M328 185h30" stroke="#94A3B8" stroke-width="6" stroke-linecap="round"/>
  <rect x="142" y="246" width="104" height="46" rx="14" fill="#4C1D95"/>
  <rect x="314" y="246" width="104" height="46" rx="14" fill="#164E63"/>
  <path d="M280 208v38M246 269h68" stroke="#94A3B8" stroke-width="6" stroke-linecap="round"/>
  <text x="153" y="191" fill="#E0E7FF" font-family="monospace" font-size="15" font-weight="700">Source</text>
  <text x="259" y="191" fill="#DBEAFE" font-family="monospace" font-size="15" font-weight="700">AST</text>
  <text x="386" y="191" fill="#CFFAFE" font-family="monospace" font-size="15" font-weight="700">AIR</text>
  <text x="166" y="275" fill="#EDE9FE" font-family="monospace" font-size="15" font-weight="700">Interp</text>
  <text x="348" y="275" fill="#CCFBF1" font-family="monospace" font-size="15" font-weight="700">CIL</text>
</svg>
EOF

# --------------------------------------------------------------------
# Home page
# --------------------------------------------------------------------

write_file "docs/index.md" <<'EOF'
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
EOF

# --------------------------------------------------------------------
# Stub generator
# --------------------------------------------------------------------

create_stub() {
    local path="$1"
    local title="$2"
    local purpose="$3"

    write_file "$path" <<EOF
---
title: $title
description: $purpose
---

# $title

<div class="placeholder-box">

This page is a documentation placeholder.

**Purpose:** $purpose

</div>

## What this page should explain

- The practical goal of this topic.
- The minimal example a developer should run or read.
- The important concepts needed before going deeper.
- Links to the next page for gradual learning.

## Status

Content is not written yet.

EOF
}

create_stub "docs/start/index.md" "Start Here" "Give developers the fastest path from zero to a running Wist program."
create_stub "docs/start/what-is-universal-toolchain.md" "What is UniversalToolchain?" "Explain UniversalToolchain as a modular .NET framework for building DSLs."
create_stub "docs/start/what-is-wist.md" "What is Wist?" "Explain Wist as the reference language built on top of UniversalToolchain."
create_stub "docs/start/installation.md" "Installation" "Show how to clone, build, and prepare the project locally."
create_stub "docs/start/first-program.md" "First Program" "Run the smallest useful Wist program and explain what happened internally."
create_stub "docs/start/mental-model.md" "Mental Model" "Explain the source to backend pipeline without overwhelming the reader."

create_stub "docs/wist/index.md" "Wist Overview" "Introduce Wist as a practical language used to demonstrate UniversalToolchain."
create_stub "docs/wist/syntax-tour.md" "Syntax Tour" "Walk through Wist syntax with small examples."
create_stub "docs/wist/numbers.md" "Numbers" "Document numeric literals and arithmetic behavior."
create_stub "docs/wist/variables.md" "Variables" "Document variable declarations, assignments, and lookup rules."
create_stub "docs/wist/conditions.md" "Conditions" "Document if/else and boolean conditions."
create_stub "docs/wist/loops.md" "Loops" "Document loop constructs and common examples."
create_stub "docs/wist/scopes.md" "Scopes" "Explain block scopes and variable visibility."
create_stub "docs/wist/examples.md" "Examples" "Collect complete Wist programs."

create_stub "docs/build-dsls/index.md" "Build DSLs" "Show how to compose existing modules into a new dialect."
create_stub "docs/build-dsls/dialect-files.md" "Dialect Files" "Document the dialect file format and its role."
create_stub "docs/build-dsls/module-composition.md" "Module Composition" "Explain how modules combine into a language."
create_stub "docs/build-dsls/minimal-dsl.md" "Minimal DSL" "Build the smallest useful DSL from existing modules."
create_stub "docs/build-dsls/backend-selection.md" "Backend Selection" "Explain interpreter, CIL, and backend choice."
create_stub "docs/build-dsls/testing-dsl.md" "Testing a DSL" "Show how to test dialect behavior and backend parity."

create_stub "docs/write-modules/index.md" "Write Modules" "Introduce language module development."
create_stub "docs/write-modules/frontend-module.md" "Frontend Module" "Explain frontend module responsibilities."
create_stub "docs/write-modules/parser-extension.md" "Parser Extension" "Show how parser behavior is extended."
create_stub "docs/write-modules/ast-nodes.md" "AST Nodes" "Explain AST node design and ownership."
create_stub "docs/write-modules/bytecode-generation.md" "Bytecode Generation" "Show how a feature lowers into bytecode."
create_stub "docs/write-modules/semantic-tags.md" "Semantic Tags" "Explain why bytecode tags exist and how they support later stages."
create_stub "docs/write-modules/ordering-and-priority.md" "Ordering and Priority" "Explain deterministic module ordering and parser priority."
create_stub "docs/write-modules/testing-module.md" "Testing a Module" "Show module-level and parity tests."

create_stub "docs/internals/index.md" "Internals Overview" "Give a map of the compiler pipeline and runtime architecture."
create_stub "docs/internals/pipeline.md" "Pipeline" "Explain source to execution pipeline."
create_stub "docs/internals/lexer.md" "Lexer" "Document lexing responsibilities and extension points."
create_stub "docs/internals/parser.md" "Parser" "Document parser architecture and extension points."
create_stub "docs/internals/ast.md" "AST" "Explain AST structure and translation responsibilities."
create_stub "docs/internals/bytecode.md" "Bytecode" "Explain bytecode as a semantic intermediate layer."
create_stub "docs/internals/air.md" "AIR" "Explain Abstract IR and backend-independent lowering."
create_stub "docs/internals/backends.md" "Backends" "Explain interpreter and CIL execution backends."
create_stub "docs/internals/intrinsics.md" "Intrinsics" "Document intrinsic capabilities and backend support."
create_stub "docs/internals/optimizers.md" "Optimizers" "Explain optimization passes and their contracts."
create_stub "docs/internals/semantic-parity.md" "Semantic Parity" "Explain why interpreter and compiled execution must agree."
create_stub "docs/internals/dependency-injection.md" "Dependency Injection" "Explain service registration and deterministic module discovery."

create_stub "docs/reference/index.md" "Reference Overview" "Index strict reference pages for advanced users."
create_stub "docs/reference/dialect-reference.md" "Dialect Reference" "Document the dialect file format precisely."
create_stub "docs/reference/module-reference.md" "Module Reference" "List built-in modules and their responsibilities."
create_stub "docs/reference/bytecode-reference.md" "Bytecode Reference" "List bytecode instructions and semantic tags."
create_stub "docs/reference/air-reference.md" "AIR Reference" "List AIR nodes, operations, and contracts."
create_stub "docs/reference/intrinsics-reference.md" "Intrinsics Reference" "List supported intrinsics and backend capabilities."
create_stub "docs/reference/backend-contracts.md" "Backend Contracts" "Document what each backend must support."
create_stub "docs/reference/project-rules.md" "Project Rules" "Document coding, testing, and architecture rules."

# --------------------------------------------------------------------
# GitHub Actions workflow
# --------------------------------------------------------------------

write_file ".github/workflows/deploy-docs.yml" <<'EOF'
name: Deploy documentation to GitHub Pages

on:
  push:
    branches:
      - master
      - main
    paths:
      - 'docs/**'
      - 'package.json'
      - 'package-lock.json'
      - '.github/workflows/deploy-docs.yml'
  workflow_dispatch:

permissions:
  contents: read
  pages: write
  id-token: write

concurrency:
  group: github-pages
  cancel-in-progress: false

jobs:
  build:
    runs-on: ubuntu-latest

    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup Node
        uses: actions/setup-node@v4
        with:
          node-version: 22
          cache: npm

      - name: Install dependencies
        run: npm ci || npm install

      - name: Build documentation
        run: npm run docs:build

      - name: Configure Pages
        uses: actions/configure-pages@v5

      - name: Upload Pages artifact
        uses: actions/upload-pages-artifact@v3
        with:
          path: docs/.vitepress/dist

  deploy:
    environment:
      name: github-pages
      url: ${{ steps.deployment.outputs.page_url }}

    runs-on: ubuntu-latest
    needs: build

    steps:
      - name: Deploy to GitHub Pages
        id: deployment
        uses: actions/deploy-pages@v4
EOF

# --------------------------------------------------------------------
# Git ignore
# --------------------------------------------------------------------

touch .gitignore

if ! grep -q "^node_modules/$" .gitignore; then
    printf "\nnode_modules/\n" >> .gitignore
fi

if ! grep -q "^docs/.vitepress/dist/$" .gitignore; then
    printf "docs/.vitepress/dist/\n" >> .gitignore
fi

if ! grep -q "^docs/.vitepress/cache/$" .gitignore; then
    printf "docs/.vitepress/cache/\n" >> .gitignore
fi

log "update: .gitignore"

# --------------------------------------------------------------------
# Optional install
# --------------------------------------------------------------------

if [[ "$INSTALL" == "1" ]]; then
    if command -v npm >/dev/null 2>&1; then
        npm install
    else
        warn "npm is not available. Install Node.js/npm first."
    fi
fi

log ""
log "Done."
log ""
log "Next commands:"
log "  npm install"
log "  npm run docs:dev"
log ""
log "Build:"
log "  npm run docs:build"
log ""
log "GitHub Pages:"
log "  Push the generated files."
log "  In repository settings, set Pages source to GitHub Actions."
log ""
log "If the deployed site has broken CSS/assets, rerun with the correct base path:"
log "  BASE_PATH=/${REPO_NAME}/ bash create-docs-site.sh"
