import { defineConfig } from 'vitepress'

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
            { text: 'Start', link: '/start/' },
            { text: 'Language', link: '/wist/' },
            { text: 'Dialects', link: '/build-dsls/' },
            { text: 'Modules', link: '/write-modules/' },
            { text: 'Internals', link: '/internals/' },
            { text: 'Reference', link: '/reference/' },
            { text: 'GitHub', link: 'https://github.com/Misha1302/Wist2' }
        ],

        search: {
            provider: 'local'
        },

        outline: {
            level: [2, 3],
            label: 'On this page'
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
