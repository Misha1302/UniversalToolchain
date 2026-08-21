import { defineConfig } from 'vitepress'

const wistLanguage = {
    name: 'wist',
    scopeName: 'source.wist',
    patterns: [
        { name: 'comment.line.double-slash.wist', match: '//.*$' },
        { name: 'string.quoted.double.wist', begin: '"', end: '"', patterns: [{ name: 'constant.character.escape.wist', match: '\\\\.' }] },
        { name: 'constant.numeric.wist', match: '\\b\\d+(?:\\.\\d+)?\\b' },
        { name: 'constant.language.boolean.wist', match: '\\b(?:true|false|null)\\b' },
        { name: 'keyword.control.wist', match: '\\b(?:if|else|for|while|let|return)\\b' },
        { name: 'keyword.operator.wist', match: '(?:==|!=|<=|>=|&&|\\|\\||[+\\-*/%=<>])' }
    ]
}

const startSidebar = [
    {
        text: 'Wist Application Developer',
        collapsed: false,
        items: [
            { text: 'Start Here', link: '/start/' },
            { text: 'Installation', link: '/start/installation' },
            { text: 'First Program', link: '/start/first-program' },
            { text: 'Production Integration', link: '/start/production-integration' },
            { text: 'Use-case Recipes', link: '/start/use-case-recipes' },
            { text: 'CLI Reference', link: '/start/cli-reference' },
            { text: 'Mental Model', link: '/start/mental-model' }
        ]
    },
    {
        text: 'Wist Language',
        collapsed: true,
        items: [
            { text: 'Language Overview', link: '/wist/' },
            { text: 'Syntax Tour', link: '/wist/syntax-tour' },
            { text: 'Numbers', link: '/wist/numbers' },
            { text: 'Variables', link: '/wist/variables' },
            { text: 'Conditions', link: '/wist/conditions' },
            { text: 'Loops', link: '/wist/loops' },
            { text: 'Scopes', link: '/wist/scopes' },
            { text: 'Examples', link: '/wist/examples' }
        ]
    },
    {
        text: 'Concepts and Alternatives',
        collapsed: true,
        items: [
            { text: 'What is UniversalToolchain?', link: '/start/what-is-universal-toolchain' },
            { text: 'What is Wist?', link: '/start/what-is-wist' },
            { text: 'Why This Exists', link: '/why-this-exists' },
            { text: 'Alternatives', link: '/alternatives' }
        ]
    },
    {
        text: 'Host Boundaries',
        collapsed: true,
        items: [
            { text: 'Diagnostics', link: '/reference/diagnostics' },
            { text: 'Lifecycle, Concurrency and Privacy', link: '/reference/lifecycle-concurrency-privacy' },
            { text: 'Performance Model', link: '/reference/performance-model' },
            { text: 'Documentation Rules', link: '/reference/documentation-rules' },
            { text: 'Contributing', link: '/CONTRIBUTING' },
            { text: 'Security', link: '/SECURITY' },
            { text: 'Current Limitations', link: '/limitations' }
        ]
    }
]

const languageAuthorSidebar = [
    {
        text: 'External Language Author',
        collapsed: false,
        items: [
            { text: 'Overview', link: '/language-authoring/' },
            { text: 'Quickstart', link: '/language-authoring/quickstart' },
            { text: 'Packages and Contributions', link: '/language-authoring/package-model' },
            { text: 'Planning and Diagnostics', link: '/language-authoring/contribution-planning' },
            { text: 'Typed Artifact Routing', link: '/language-authoring/artifact-routing' },
            { text: 'Runtime Lifecycle and Policy', link: '/language-authoring/runtime-lifecycle' },
            { text: 'Testing and Templates', link: '/language-authoring/testing-and-templates' },
            { text: 'Versioning and Migrations', link: '/language-authoring/versioning-and-migrations' }
        ]
    },
    {
        text: 'Deep Reference',
        collapsed: true,
        items: [
            { text: 'SDK Architecture', link: '/architecture/external-language-authoring-sdk' },
            { text: 'Architecture Learning Path', link: '/architecture/learning-path' },
            { text: 'Physical Project Map', link: '/architecture/project-map' },
            { text: 'Lowering and Route Walkthrough', link: '/architecture/lowering-walkthrough' },
            { text: 'Lifecycle, Concurrency and Privacy', link: '/reference/lifecycle-concurrency-privacy' },
            { text: 'Language Authoring Evidence', link: '/evidence/language-authoring-alpha' }
        ]
    }
]

const dialectSidebar = [
    {
        text: 'Wist Dialect Author',
        collapsed: false,
        items: [
            { text: 'Overview', link: '/build-dsls/' },
            { text: 'Embedding in .NET', link: '/build-dsls/embedding-dotnet' },
            { text: 'Dialect Files', link: '/build-dsls/dialect-files' },
            { text: 'Minimal Dialect', link: '/build-dsls/minimal-dsl' },
            { text: 'Module Composition', link: '/build-dsls/module-composition' },
            { text: 'Backend Selection', link: '/build-dsls/backend-selection' },
            { text: 'Custom Dialect Fast Invocation', link: '/build-dsls/custom-dialect-fast-invocation' },
            { text: 'Testing a Dialect', link: '/build-dsls/testing-dsl' },
            { text: 'Restricted Dialect Security', link: '/build-dsls/restricted-dsl-security' }
        ]
    },
    {
        text: 'Reference',
        collapsed: true,
        items: [
            { text: 'Dialect Reference', link: '/reference/dialect-reference' },
            { text: 'Runtime Profiles', link: '/reference/runtime-profiles' },
            { text: 'Composition Explainability', link: '/architecture/composition-explain-plan' },
            { text: 'Dialect Groups', link: '/dialect-groups' },
            { text: 'Security', link: '/SECURITY' }
        ]
    }
]

const moduleSidebar = [
    {
        text: 'Wist Compiler Module Authoring',
        collapsed: false,
        items: [
            { text: 'Overview and Status', link: '/write-modules/' },
            { text: 'Choose an Extension Type', link: '/write-modules/choose-extension-type' },
            { text: 'Create Your First Module', link: '/write-modules/create-your-first-module' },
            { text: 'Frontend Module', link: '/write-modules/frontend-module' },
            { text: 'Parser Extension', link: '/write-modules/parser-extension' },
            { text: 'AST Nodes', link: '/write-modules/ast-nodes' },
            { text: 'Bytecode Generation', link: '/write-modules/bytecode-generation' },
            { text: 'Semantic Tags', link: '/write-modules/semantic-tags' },
            { text: 'Ordering and Priority', link: '/write-modules/ordering-and-priority' },
            { text: 'Writing Backends', link: '/write-modules/writing-backends' },
            { text: 'Testing a Module', link: '/write-modules/testing-module' }
        ]
    },
    {
        text: 'Compiler Contracts',
        collapsed: true,
        items: [
            { text: 'Module Reference', link: '/reference/module-reference' },
            { text: 'Module Contracts', link: '/reference/module-contracts' },
            { text: 'Bytecode Reference', link: '/reference/bytecode-reference' },
            { text: 'AIR Reference', link: '/reference/air-reference' },
            { text: 'Backend Contracts', link: '/reference/backend-contracts' }
        ]
    }
]

const architectureSidebar = [
    {
        text: 'Framework Architecture',
        collapsed: false,
        items: [
            { text: 'Architecture Learning Path', link: '/architecture/learning-path' },
            { text: 'Physical Project Map', link: '/architecture/project-map' },
            { text: 'Lowering and Route Walkthrough', link: '/architecture/lowering-walkthrough' },
            { text: 'Wist Pipeline', link: '/internals/pipeline' },
            { text: 'Bytecode and AIR', link: '/architecture/bytecode-and-air' },
            { text: 'Runtime Composition', link: '/current-canonical-runtime-pipeline' },
            { text: 'Composition Explainability', link: '/architecture/composition-explain-plan' },
            { text: 'Backends and Parity', link: '/architecture/backends-and-parity' },
            { text: 'IR Routing Foundation', link: '/architecture/ir-routing-foundation' },
            { text: 'External Language Authoring SDK', link: '/architecture/external-language-authoring-sdk' },
            { text: 'Callable-first SSA', link: '/architecture/callable-first-ssa' },
            { text: 'SSA Coverage Matrix', link: '/architecture/ssa-coverage-matrix' },
            { text: 'SSA Route Tests', link: '/testing/ssa-route-tests' },
            { text: 'Debug Trace v2', link: '/architecture/debug-trace-v2' }
        ]
    },
    {
        text: 'Implementation Internals',
        collapsed: true,
        items: [
            { text: 'Internals Home', link: '/internals/' },
            { text: 'Lexer', link: '/internals/lexer' },
            { text: 'Parser', link: '/internals/parser' },
            { text: 'AST', link: '/internals/ast' },
            { text: 'Bytecode', link: '/internals/bytecode' },
            { text: 'AIR', link: '/internals/air' },
            { text: 'Optimizers', link: '/internals/optimizers' },
            { text: 'Backends', link: '/internals/backends' },
            { text: 'Semantic Parity', link: '/internals/semantic-parity' },
            { text: 'Dependency Injection', link: '/internals/dependency-injection' }
        ]
    }
]

const referenceSidebar = [
    {
        text: 'Reference',
        collapsed: false,
        items: [
            { text: 'Reference Home', link: '/reference/' },
            { text: 'Diagnostics', link: '/reference/diagnostics' },
            { text: 'Lifecycle, Concurrency and Privacy', link: '/reference/lifecycle-concurrency-privacy' },
            { text: 'Dialect Reference', link: '/reference/dialect-reference' },
            { text: 'Runtime Profiles', link: '/reference/runtime-profiles' },
            { text: 'Module Reference', link: '/reference/module-reference' },
            { text: 'Module Contracts', link: '/reference/module-contracts' },
            { text: 'Bytecode Reference', link: '/reference/bytecode-reference' },
            { text: 'AIR Reference', link: '/reference/air-reference' },
            { text: 'Intrinsics Reference', link: '/reference/intrinsics-reference' },
            { text: 'Backend Contracts', link: '/reference/backend-contracts' },
            { text: 'Debug Trace Schema', link: '/reference/debug-trace-schema' },
            { text: 'Benchmark Methodology', link: '/reference/benchmark-methodology' },
            { text: 'Performance Model', link: '/reference/performance-model' },
            { text: 'Documentation Rules', link: '/reference/documentation-rules' },
            { text: 'Contributing', link: '/CONTRIBUTING' }
        ]
    }
]

const evidenceSidebar = [
    {
        text: 'Evidence and Releases',
        collapsed: false,
        items: [
            { text: 'Evidence Home', link: '/evidence/' },
            { text: 'Maintainer and Release Guide', link: '/evidence/maintainer-guide' },
            { text: 'Verification Snapshot', link: '/evidence/current-verification' },
            { text: 'Language Authoring Alpha', link: '/evidence/language-authoring-alpha' },
            { text: 'Wist 0.1.0-alpha.7 Candidate Stability', link: '/evidence/wist-stability-v0.1.0-alpha.7' },
            { text: 'External Authoring Hardening', link: '/releases/external-language-authoring-hardening-2026-07-21' },
            { text: 'Composition Hardening', link: '/releases/external-language-composition-hardening-2026-07-22' },
            { text: 'Contribution Planning', link: '/releases/external-language-contribution-planning-2026-07-21' },
            { text: 'SSA Route Correctness', link: '/releases/ssa-route-correctness-2026-07-04' },
            { text: 'Wist 0.1.0-alpha.4 Immutable Pruning', link: '/releases/v0.1.0-alpha.4-immutable-pruning' },
            { text: 'Wist 0.1.0-alpha.3 Hardening', link: '/releases/v0.1.0-alpha.3-boundary-hardening' },
            { text: 'Wist 0.1.0-alpha.1', link: '/releases/v0.1.0-alpha.1' }
        ]
    },
    {
        text: 'Current Boundaries',
        collapsed: true,
        items: [
            { text: 'Architecture Status', link: '/CURRENT_ARCHITECTURE_STATUS' },
            { text: 'Security', link: '/SECURITY' },
            { text: 'Limitations', link: '/limitations' }
        ]
    }
]

const homeSidebar = [
    {
        text: 'Choose a Documentation Route',
        collapsed: false,
        items: [
            { text: 'Documentation Home', link: '/' },
            { text: 'Wist Application Developer', link: '/start/' },
            { text: 'External Language Author', link: '/language-authoring/' },
            { text: 'Wist Dialect Author', link: '/build-dsls/' },
            { text: 'Wist Compiler Contributor', link: '/write-modules/' },
            { text: 'Framework Architecture', link: '/architecture/project-map' },
            { text: 'Reference', link: '/reference/' },
            { text: 'Evidence and Releases', link: '/evidence/' }
        ]
    },
    {
        text: 'Status and Safety',
        collapsed: true,
        items: [
            { text: 'Architecture Status', link: '/CURRENT_ARCHITECTURE_STATUS' },
            { text: 'Security', link: '/SECURITY' },
            { text: 'Limitations', link: '/limitations' }
        ]
    }
]

export default defineConfig({
    title: 'UniversalToolchain',
    description: 'Task-oriented developer documentation for Wist and external UniversalToolchain language authoring.',
    base: '/Wist2/',
    cleanUrls: true,
    lastUpdated: true,
    srcExclude: [
        'archive/**',
        'reviews/**',
        'proposals/**',
        'talks/**',
        'maintainers/**',
        'vision/**'
    ],
    markdown: {
        languages: [wistLanguage as any]
    },
    head: [
        ['meta', { name: 'theme-color', content: '#0f172a' }],
        ['link', { rel: 'icon', type: 'image/svg+xml', href: '/Wist2/logo.svg' }]
    ],
    themeConfig: {
        logo: '/logo.svg',
        nav: [
            { text: 'Wist', link: '/start/' },
            { text: 'Language Authoring', link: '/language-authoring/' },
            { text: 'Architecture', link: '/architecture/project-map' },
            { text: 'Reference', link: '/reference/' },
            { text: 'Evidence', link: '/evidence/' },
            { text: 'GitHub', link: 'https://github.com/Misha1302/UniversalToolchain' }
        ],
        search: { provider: 'local' },
        sidebar: {
            '/start/': startSidebar,
            '/wist/': startSidebar,
            '/language-authoring/': languageAuthorSidebar,
            '/build-dsls/': dialectSidebar,
            '/write-modules/': moduleSidebar,
            '/architecture/': architectureSidebar,
            '/internals/': architectureSidebar,
            '/reference/': referenceSidebar,
            '/evidence/': evidenceSidebar,
            '/releases/': evidenceSidebar,
            '/': homeSidebar
        },
        outline: { level: [2, 3], label: 'On this page' },
        docFooter: { prev: 'Previous', next: 'Next' },
        socialLinks: [{ icon: 'github', link: 'https://github.com/Misha1302/UniversalToolchain' }],
        editLink: {
            pattern: 'https://github.com/Misha1302/UniversalToolchain/edit/master/docs/:path',
            text: 'Edit this page on GitHub'
        },
        footer: {
            message: 'Public developer manual. Internal reviews, proposals and maintainer material live under internal-docs/.',
            copyright: 'UniversalToolchain documentation'
        }
    }
})
