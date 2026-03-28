// Модуль для построения секции лексем
class LexemesBuilder {
    static display(lexemesContent, sourceCode) {
        const lexemesContainer = document.getElementById('lexemes-content');

        if (!lexemesContainer) return;

        lexemesContainer.innerHTML = '';

        const lexemesByLine = this.groupLexemesByLine(lexemesContent, sourceCode);

        Object.keys(lexemesByLine)
            .sort((a, b) => parseInt(a) - parseInt(b))
            .forEach(lineNumber => {
                this.createLineElement(lexemesContainer, lineNumber, lexemesByLine[lineNumber]);
            });
    }

    static groupLexemesByLine(lexemesContent, sourceCode) {
        const lines = lexemesContent.split('\n').filter(line => line.trim() !== '');
        const lexemesByLine = {};
        const sourceLines = sourceCode.split('\n');

        lines.forEach(line => {
            const lexeme = LogParser.parseLexeme(line);
            if (!lexeme) return;

            const lineNumber = lexeme.line;

            if (!lexemesByLine[lineNumber]) {
                lexemesByLine[lineNumber] = {
                    sourceCode: sourceLines[lineNumber - 1] || '',
                    lexemes: []
                };
            }

            lexemesByLine[lineNumber].lexemes.push(lexeme);
        });

        return lexemesByLine;
    }

    static createLineElement(container, lineNumber, lineData) {
        const lineElement = document.createElement('div');
        lineElement.className = 'lexeme-line';

        const headerElement = this.createLineHeader(lineNumber, lineData.sourceCode);
        const lexemesListElement = this.createLexemesList(lineData.lexemes);

        lineElement.appendChild(headerElement);
        lineElement.appendChild(lexemesListElement);

        container.appendChild(lineElement);
    }

    static createLineHeader(lineNumber, sourceCode) {
        const headerElement = document.createElement('div');
        headerElement.className = 'lexeme-line-header';

        const lineNumberElement = document.createElement('div');
        lineNumberElement.className = 'lexeme-line-number';
        lineNumberElement.textContent = `Строка ${lineNumber}`;

        const sourceCodeElement = document.createElement('div');
        sourceCodeElement.className = 'lexeme-source-code';
        sourceCodeElement.textContent = sourceCode || '';

        headerElement.appendChild(lineNumberElement);
        headerElement.appendChild(sourceCodeElement);

        return headerElement;
    }

    static createLexemesList(lexemes) {
        const lexemesListElement = document.createElement('div');
        lexemesListElement.className = 'lexemes-list';

        lexemes.forEach(lexeme => {
            const lexemeElement = this.createLexemeElement(lexeme);
            lexemesListElement.appendChild(lexemeElement);
        });

        return lexemesListElement;
    }

    static createLexemeElement(lexeme) {
        const lexemeElement = document.createElement('div');
        lexemeElement.className = 'lexeme-item';

        const typeElement = document.createElement('div');
        typeElement.className = 'lexeme-type';
        typeElement.textContent = lexeme.type;

        const valueElement = document.createElement('div');
        valueElement.className = 'lexeme-value';
        valueElement.textContent = lexeme.value;

        const positionElement = document.createElement('div');
        positionElement.className = 'lexeme-position';
        positionElement.textContent = `Позиция: ${lexeme.position}`;

        lexemeElement.appendChild(typeElement);
        lexemeElement.appendChild(valueElement);
        lexemeElement.appendChild(positionElement);

        return lexemeElement;
    }
}