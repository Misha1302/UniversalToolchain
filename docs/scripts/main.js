// Основной модуль приложения
class LogViewer {
    constructor() {
        this.currentLogs = null;
        this.astBuilder = new ASTBuilder();
        this.cilBuilder = new CILBuilder();
        this.init();
    }

    init() {
        UIController.setupNavigation();
        UIController.setupFileUpload((content) => this.parseLogFile(content));
        
        // Загружаем пример логов по умолчанию
        this.loadDefaultLogs();
    }

    async loadDefaultLogs() {
        try {
            const response = await fetch('sample-logs.txt');
            if (response.ok) {
                const data = await response.text();
                this.parseLogFile(data);
            }
        } catch (error) {
            console.log('Пример логов не загружен. Вы можете загрузить свой файл.');
        }
    }

    parseLogFile(content) {
        this.currentLogs = LogParser.parseLogFile(content);
        this.displayLogs();
    }

    displayLogs() {
        if (!this.currentLogs) return;

        UIController.displaySection('code', this.currentLogs.code);
        LexemesBuilder.display(this.currentLogs.lexemes, this.currentLogs.code);
        this.astBuilder.display(this.currentLogs.ast);
        BytecodeBuilder.display(this.currentLogs.bytecode);
        this.cilBuilder.display(this.currentLogs.dotnet);
    }
}

// Инициализация приложения
document.addEventListener('DOMContentLoaded', () => {
    new LogViewer();
});