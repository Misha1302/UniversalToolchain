export function decodeBase64Pattern(encoded: string): string {
    try {
        // Используем decodeURIComponent для правильной работы с Unicode
        return decodeURIComponent(atob(encoded).split('').map(c =>
            '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2)
        ).join(''));
    } catch (error) {
        console.error('Ошибка декодирования Base64:', error);
        return '';
    }
}

export function encodeBase64Pattern(decoded: string): string {
    try {
        return btoa(encodeURIComponent(decoded).replace(/%([0-9A-F]{2})/g,
            (match, p1) => String.fromCharCode(parseInt(p1, 16))));
    } catch (error) {
        console.error('Ошибка кодирования Base64:', error);
        return '';
    }
}

export function isBase64(str: string): boolean {
    try {
        return btoa(atob(str)) === str;
    } catch (error) {
        return false;
    }
}
