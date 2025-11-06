// scripts/split-view.js - Простой модуль для разделенного просмотра
class SplitViewManager {
    constructor() {
        this.isActive = false;
        this.init();
    }

    init() {
        // Назначаем обработчик для кнопки разделения
        document.getElementById('split-view-btn').addEventListener('click', () => {
            this.toggleSplitView();
        });

        // Назначаем обработчики для кнопок закрытия
        document.querySelectorAll('.close-split-view').forEach(btn => {
            btn.addEventListener('click', () => {
                this.closeSplitView();
            });
        });

        console.log('SplitViewManager initialized');
    }

    toggleSplitView() {
        if (this.isActive) {
            this.closeSplitView();
        } else {
            this.openSplitView();
        }
    }

    openSplitView() {
        // Получаем выбранные секции
        const leftSection = document.getElementById('left-panel-select').value;
        const rightSection = document.getElementById('right-panel-select').value;

        // Проверяем, что секции разные
        if (leftSection === rightSection) {
            alert('Пожалуйста, выберите разные секции для разделенного просмотра');
            return;
        }

        // Обновляем заголовки панелей
        document.getElementById('left-panel-title').textContent = this.getSectionTitle(leftSection);
        document.getElementById('right-panel-title').textContent = this.getSectionTitle(rightSection);

        // Загружаем контент в панели
        this.loadPanelContent('left', leftSection);
        this.loadPanelContent('right', rightSection);

        // Показываем контейнер разделенного просмотра
        document.getElementById('split-view-container').style.display = 'grid';
        
        // Скрываем обычные секции
        document.querySelectorAll('.content-section').forEach(section => {
            section.style.display = 'none';
        });

        // Скрываем навигацию и контролы
        document.querySelector('.section-nav').style.display = 'none';
        document.querySelector('.split-controls').style.display = 'none';

        this.isActive = true;
        console.log('Split view opened');
    }

    closeSplitView() {
        // Показываем обычные секции
        document.querySelectorAll('.content-section').forEach(section => {
            section.style.display = 'block';
        });

        // Показываем навигацию и контролы
        document.querySelector('.section-nav').style.display = 'flex';
        document.querySelector('.split-controls').style.display = 'flex';

        // Скрываем контейнер разделенного просмотра
        document.getElementById('split-view-container').style.display = 'none';

        // Восстанавливаем активную секцию
        const activeSection = document.querySelector('.content-section.active');
        if (activeSection) {
            document.querySelectorAll('.content-section').forEach(s => s.classList.remove('active'));
            activeSection.classList.add('active');
            
            document.querySelectorAll('.nav-btn').forEach(btn => btn.classList.remove('active'));
            document.querySelector(`[data-section="${activeSection.id}"]`).classList.add('active');
        }

        this.isActive = false;
        console.log('Split view closed');
    }

    getSectionTitle(section) {
        const titles = {
            code: 'Исходный код',
            lexemes: 'Лексемы',
            ast: 'AST',
            bytecode: 'Байткод',
            dotnet: '.NET код (CIL)'
        };
        return titles[section] || section;
    }

    loadPanelContent(side, section) {
        const container = document.getElementById(`${side}-panel-content`);
        if (!container || !window.logViewer || !window.logViewer.currentLogs) {
            console.error('Cannot load panel content');
            return;
        }

        const logs = window.logViewer.currentLogs;
        
        // Очищаем контейнер
        container.innerHTML = '';

        switch(section) {
            case 'code':
                this.loadCodeSection(container, logs.code);
                break;
                
            case 'lexemes':
                this.loadLexemesSection(container, logs.lexemes, logs.code);
                break;
                
            case 'ast':
                this.loadASTSection(container, logs.ast);
                break;
                
            case 'bytecode':
                this.loadBytecodeSection(container, logs.bytecode);
                break;
                
            case 'dotnet':
                this.loadDotnetSection(container, logs.dotnet);
                break;
        }
    }

    loadCodeSection(container, content) {
        container.innerHTML = `
            <div class="code-block">
                <div class="line-numbers"></div>
                <pre>${content}</pre>
            </div>
        `;
        
        // Добавляем номера строк
        const lineCount = content.split('\n').length;
        const lineNumbersElement = container.querySelector('.line-numbers');
        lineNumbersElement.innerHTML = Array.from({length: lineCount}, (_, i) => 
            `<div>${i + 1}</div>`
        ).join('');
    }

    loadLexemesSection(container, lexemesContent, sourceCode) {
        container.innerHTML = '<div class="lexemes-container"></div>';
        const lexemesContainer = container.querySelector('.lexemes-container');
        
        // Используем существующий построитель лексем
        if (typeof LexemesBuilder !== 'undefined') {
            LexemesBuilder.display(lexemesContent, sourceCode, lexemesContainer);
        } else {
            lexemesContainer.innerHTML = '<p>Лексемы не загружены</p>';
        }
    }

    loadASTSection(container, content) {
        container.innerHTML = `
            <div class="ast-controls">
                <button class="control-btn split-zoom-in">+</button>
                <button class="control-btn split-zoom-out">-</button>
                <button class="control-btn split-reset-view">Сброс</button>
            </div>
            <div class="ast-container" style="max-height: none; height: auto; overflow: visible; padding: 0;">
                <div class="ast-tree"></div>
            </div>
        `;

        // Загружаем AST
        const astTree = container.querySelector('.ast-tree');
        if (typeof ASTBuilder !== 'undefined') {
            const astBuilder = new ASTBuilder();
            const astData = LogParser.parseAST(content);
            astBuilder.renderTree(astData, astTree);
            
            // Настраиваем контролы масштабирования
            this.setupASTControls(astBuilder, container);
        } else {
            astTree.innerHTML = '<p>AST не загружено</p>';
        }
    }

    setupASTControls(astBuilder, container) {
        container.querySelector('.split-zoom-in').addEventListener('click', () => astBuilder.zoomIn());
        container.querySelector('.split-zoom-out').addEventListener('click', () => astBuilder.zoomOut());
        container.querySelector('.split-reset-view').addEventListener('click', () => astBuilder.resetView());
    }

    loadBytecodeSection(container, content) {
        container.innerHTML = '<div class="bytecode-container"></div>';
        const bytecodeContainer = container.querySelector('.bytecode-container');
        
        // Используем существующий построитель байткода
        if (typeof BytecodeBuilder !== 'undefined') {
            BytecodeBuilder.display(content, bytecodeContainer);
        } else {
            bytecodeContainer.innerHTML = '<p>Байткод не загружен</p>';
        }
    }

    loadDotnetSection(container, content) {
        container.innerHTML = '<div class="cil-container"></div>';
        const cilContainer = container.querySelector('.cil-container');
        
        // Используем существующий построитель CIL
        if (window.logViewer && window.logViewer.cilBuilder) {
            window.logViewer.cilBuilder.display(content, cilContainer);
        } else {
            cilContainer.innerHTML = '<p>.NET код не загружен</p>';
        }
    }

    // Обновляем контент при загрузке новых логов
    updateContent() {
        if (this.isActive) {
            const leftSection = document.getElementById('left-panel-select').value;
            const rightSection = document.getElementById('right-panel-select').value;
            
            this.loadPanelContent('left', leftSection);
            this.loadPanelContent('right', rightSection);
        }
    }
}