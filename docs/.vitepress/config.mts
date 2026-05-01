import { defineConfig } from 'vitepress'

export default defineConfig({
    title: 'UniversalToolchain',
    description: 'Developer documentation for UniversalToolchain and Wist.',
    base: '/Wist2/',
    cleanUrls: true,
    lastUpdated: true,

    themeConfig: {
        nav: [
            { text: 'Start', link: '/start/' },
            { text: 'GitHub', link: 'https://github.com/Misha1302/Wist2' }
        ],
        search: {
            provider: 'local'
        },
        editLink: {
            pattern: 'https://github.com/Misha1302/Wist2/edit/master/docs/:path',
            text: 'Edit this page on GitHub'
        }
    }
})
