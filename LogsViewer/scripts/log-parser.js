// Модуль для парсинга логов
class LogParser {
    static parseLogFile(content) {
        const sections = content.split('----------------------------------------------------------------------------------------------------');

        return {
            code: this.extractSectionContent(sections[0], 'CODE:'),
            lexemes: this.extractSectionContent(sections[1], 'LEXEMES:'),
            ast: this.extractSectionContent(sections[2], 'AST:'),
            bytecode: this.extractSectionContent(sections[3], 'BYTECODE:'),
            dotnet: this.extractSectionContent(sections[4], 'Dotnet code:')
        };
    }

    static extractSectionContent(section, header) {
        if (!section) return '';

        const headerIndex = section.indexOf(header);
        if (headerIndex === -1) return section.trim();

        return section.substring(headerIndex + header.length).trim();
    }

    static parseLexeme(lexemeLine) {
        const valueEnd = lexemeLine.indexOf(' (');
        if (valueEnd === -1) return null;

        const value = lexemeLine.substring(0, valueEnd);

        const typeStart = valueEnd + 2;
        const typeEnd = lexemeLine.indexOf(':', typeStart);
        if (typeEnd === -1) return null;

        const type = lexemeLine.substring(typeStart, typeEnd);

        const positionStart = lexemeLine.indexOf('at ');
        if (positionStart === -1) return null;

        const position = lexemeLine.substring(positionStart + 3);
        const line = parseInt(position.split(':')[0]);

        return {
            value,
            type,
            position,
            line
        };
    }

    static parseASTNode(line) {
        const cleanLine = line.replace(/\s*:\s*\[\s*$/, '');

        const typeEnd = cleanLine.indexOf(':');
        if (typeEnd === -1) return null;

        const type = cleanLine.substring(0, typeEnd).trim();
        const rest = cleanLine.substring(typeEnd + 1).trim();

        let value = '';
        let position = '';

        const valueMatch = rest.match(/^(.*?)\s+at\s+(\d+:\d+)/);
        if (valueMatch) {
            value = valueMatch[1].trim();
            position = valueMatch[2];
        } else {
            value = rest;
        }

        value = value.replace(/^\(.*?\)\s*/, '');

        return {
            type,
            value,
            position,
            children: []
        };
    }

    static parseAST(astContent) {
        const lines = astContent.split('\n').filter(line => line.trim() !== '');

        const root = {
            type: 'Scope',
            value: '',
            position: '',
            children: []
        };

        let currentLevel = 0;
        const stack = [{node: root, level: -1}];

        lines.forEach(line => {
            const level = (line.match(/^(\s*)/)[0].length) / 2;
            const content = line.trim();

            if (!content || content === '[' || content === ']' || content === ': [') {
                return;
            }

            const node = this.parseASTNode(content);
            if (!node) return;

            while (stack.length > 0 && stack[stack.length - 1].level >= level) {
                stack.pop();
            }

            if (stack.length > 0) {
                stack[stack.length - 1].node.children.push(node);
            }

            stack.push({node, level});
        });

        return root;
    }
}