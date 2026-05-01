import DefaultTheme from 'vitepress/theme'
import './style.css'

function isEditableTarget(target: EventTarget | null): boolean {
    if (!(target instanceof HTMLElement)) {
        return false
    }

    if (target.isContentEditable) {
        return true
    }

    return target instanceof HTMLInputElement || target instanceof HTMLTextAreaElement || target instanceof HTMLSelectElement
}

function installPhysicalSearchShortcut(): void {
    if (typeof window === 'undefined') {
        return
    }

    const windowWithFlag = window as Window & { __utPhysicalSearchShortcutInstalled?: boolean }

    if (windowWithFlag.__utPhysicalSearchShortcutInstalled) {
        return
    }

    windowWithFlag.__utPhysicalSearchShortcutInstalled = true

    window.addEventListener('keydown', (event: KeyboardEvent) => {
        const isSearchShortcut =
            (event.ctrlKey || event.metaKey) &&
            !event.altKey &&
            !event.shiftKey &&
            event.code === 'KeyK'

        if (!isSearchShortcut || isEditableTarget(event.target)) {
            return
        }

        const searchButton = document.querySelector<HTMLButtonElement>('button.DocSearch-Button')

        if (!searchButton) {
            return
        }

        event.preventDefault()
        searchButton.click()
    })
}

export default {
    ...DefaultTheme,
    enhanceApp(context) {
        DefaultTheme.enhanceApp?.(context)
        installPhysicalSearchShortcut()
    }
}
