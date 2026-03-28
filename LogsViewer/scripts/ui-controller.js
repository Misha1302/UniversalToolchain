// Модуль для управления интерфейсом
class UIController {
    static displaySection(sectionId, content) {
        const contentElement = document.getElementById(`${sectionId}-content`);
        const lineNumbersElement = document.getElementById(`${sectionId}-line-numbers`);

        if (contentElement && lineNumbersElement) {
            contentElement.textContent = content;

            const lineCount = content.split('\n').length;
            lineNumbersElement.innerHTML = Array.from({length: lineCount}, (_, i) =>
                `<div>${i + 1}</div>`
            ).join('');
        }
    }

    static switchSection(sectionId) {
        document.querySelectorAll('.nav-btn').forEach(btn => {
            btn.classList.remove('active');
        });
        document.querySelectorAll('.content-section').forEach(section => {
            section.classList.remove('active');
        });

        document.querySelector(`[data-section="${sectionId}"]`).classList.add('active');
        document.getElementById(sectionId).classList.add('active');
    }

    static setupNavigation() {
        document.querySelectorAll('.nav-btn').forEach(btn => {
            btn.addEventListener('click', (e) => {
                this.switchSection(e.target.dataset.section);
            });
        });
    }

    static setupFileUpload(callback) {
        document.getElementById('log-file').addEventListener('change', async (e) => {
            const file = e.target.files[0];
            if (!file) return;

            FileHandler.updateFileInfo(file);

            try {
                const content = await FileHandler.readFile(file);
                callback(content);
            } catch (error) {
                console.error('Ошибка при чтении файла:', error);
            }
        });
    }
}