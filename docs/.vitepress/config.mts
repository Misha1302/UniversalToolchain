import { defineConfig } from 'vitepress'

const bookSidebar = [
    {
        text: '0. Introduction',
        collapsed: false,
        items: [
            { text: '0.1 Start Here', link: '/start/' },
            { text: '0.2 What is UniversalToolchain?', link: '/start/what-is-universal-toolchain' },
            { text: '0.3 What is Wist?', link: '/start/what-is-wist' },
            { text: '0.4 Mental Model', link: '/start/mental-model' }
        ]
    },
    {
        text: '1. Getting Started',
        collapsed: false,
        items: [
            { text: '1.1 Installation', link: '/start/installation' },
            { text: '1.2 First Program', link: '/start/first-program' },
            { text: '1.3 CLI Reference', link: '/start/cli-reference' },
            { text: '1.4 Wist Overview', link: '/wist/' },
            { text: '1.5 Wist Syntax Tour', link: '/wist/syntax-tour' },
            { text: '1.6 Examples', link: '/wist/examples' }
        ]
    },
    {
        text: '2. Wist Language',
        collapsed: false,
        items: [
            { text: '2.1 Numbers', link: '/wist/numbers' },
            { text: '2.2 Variables', link: '/wist/variables' },
            { text: '2.3 Conditions', link: '/wist/conditions' },
            { text: '2.4 Loops', link: '/wist/loops' },
            { text: '2.5 Scopes', link: '/wist/scopes' }
        ]
    },
    {
        text: '3. Building DSLs',
        collapsed: false,
        items: [
            { text: '3.1 Overview', link: '/build-dsls/' },
            { text: '3.2 Dialect Files', link: '/build-dsls/dialect-files' },
            { text: '3.3 Minimal DSL', link: '/build-dsls/minimal-dsl' },
            { text: '3.4 Module Composition', link: '/build-dsls/module-composition' },
            { text: '3.5 Backend Selection', link: '/build-dsls/backend-selection' },
            { text: '3.6 Embedding in .NET', link: '/build-dsls/embedding-dotnet' },
            { text: '3.7 Custom Dialect Fast Invocation', link: '/build-dsls/custom-dialect-fast-invocation' },
            { text: '3.8 Testing a DSL', link: '/build-dsls/testing-dsl' },
            { text: '3.9 Restricted DSL Security', link: '/build-dsls/restricted-dsl-security' }
        ]
    },
    {
        text: '4. Writing Modules',
        collapsed: false,
        items: [
            { text: '4.1 Overview', link: '/write-modules/' },
            { text: '4.2 Choose an Extension Type', link: '/write-modules/choose-extension-type' },
            { text: '4.3 Create Your First Module', link: '/write-modules/create-your-first-module' },
            { text: '4.4 Runtime Manifests', link: '/write-modules/runtime-manifests' },
            { text: '4.5 Writing Backends', link: '/write-modules/writing-backends' },
            { text: '4.6 Frontend Module', link: '/write-modules/frontend-module' },
            { text: '4.7 Parser Extension', link: '/write-modules/parser-extension' },
            { text: '4.8 AST Nodes', link: '/write-modules/ast-nodes' },
            { text: '4.9 Bytecode Generation', link: '/write-modules/bytecode-generation' },
            { text: '4.10 Semantic Tags', link: '/write-modules/semantic-tags' },
            { text: '4.11 Ordering and Priority', link: '/write-modules/ordering-and-priority' },
            { text: '4.12 Testing a Module', link: '/write-modules/testing-module' }
        ]
    },
    {
        text: '5. Internals',
        collapsed: false,
        items: [
            { text: '5.1 Overview', link: '/internals/' },
            { text: '5.2 Pipeline', link: '/internals/pipeline' },
            { text: '5.3 Lexer', link: '/internals/lexer' },
            { text: '5.4 Parser', link: '/internals/parser' },
            { text: '5.5 AST', link: '/internals/ast' },
            { text: '5.6 Bytecode', link: '/internals/bytecode' },
            { text: '5.7 AIR', link: '/internals/air' },
            { text: '5.8 Backends', link: '/internals/backends' },
            { text: '5.9 Intrinsics', link: '/internals/intrinsics' },
            { text: '5.10 Optimizers', link: '/internals/optimizers' },
            { text: '5.11 Semantic Parity', link: '/internals/semantic-parity' },
            { text: '5.12 Dependency Injection', link: '/internals/dependency-injection' }
        ]
    },
    {
        text: '6. Reference',
        collapsed: false,
        items: [
            { text: '6.1 Overview', link: '/reference/' },
            { text: '6.2 Dialect Reference', link: '/reference/dialect-reference' },
            { text: '6.3 Module Reference', link: '/reference/module-reference' },
            { text: '6.4 Module Contracts', link: '/reference/module-contracts' },
            { text: '6.5 Bytecode Reference', link: '/reference/bytecode-reference' },
            { text: '6.6 AIR Reference', link: '/reference/air-reference' },
            { text: '6.7 Intrinsics Reference', link: '/reference/intrinsics-reference' },
            { text: '6.8 Backend Contracts', link: '/reference/backend-contracts' },
            { text: '6.9 Debug Trace Schema', link: '/reference/debug-trace-schema' },
            { text: '6.10 Project Rules', link: '/reference/project-rules' },
            { text: '6.11 Documentation Rules', link: '/reference/documentation-rules' }
        ]
    }
]

const bookNav = [
    { text: '0. Introduction', link: '/start/' },
    { text: '1. Getting Started', link: '/start/installation' },
    { text: '2. Wist Language', link: '/wist/' },
    { text: '3. Building DSLs', link: '/build-dsls/' },
    { text: '4. Writing Modules', link: '/write-modules/' },
    { text: '5. Internals', link: '/internals/' },
    { text: '6. Reference', link: '/reference/' }
]

export default defineConfig({
    title: 'UniversalToolchain',
    description: 'Developer documentation for UniversalToolchain and Wist.',
    base: '/Wist2/',
    cleanUrls: true,
    lastUpdated: true,

    head: [
        ['meta', { name: 'theme-color', content: '#0f172a' }],
        ['link', { rel: 'icon', type: 'image/svg+xml', href: '/Wist2/logo.svg' }]
    ],

    themeConfig: {
        logo: '/logo.svg',

        nav: [
            { text: 'Book', items: bookNav },
            { text: 'GitHub', link: 'https://github.com/Misha1302/Wist2' }
        ],

        search: {
            provider: 'local'
        },

        sidebar: bookSidebar,

        outline: {
            level: [2, 3],
            label: 'On this page'
        },

        docFooter: {
            prev: 'Previous',
            next: 'Next'
        },

        socialLinks: [
            { icon: 'github', link: 'https://github.com/Misha1302/Wist2' }
        ],

        editLink: {
            pattern: 'https://github.com/Misha1302/Wist2/edit/master/docs/:path',
            text: 'Edit this page on GitHub'
        },

        footer: {
            message: 'Built for developers who want to use, extend, or understand UniversalToolchain.',
            copyright: 'UniversalToolchain documentation'
        }
    }
})
