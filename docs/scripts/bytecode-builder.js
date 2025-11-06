// Модуль для построения визуализации байткода
class BytecodeBuilder {
    static display(bytecodeContent, customContainer) {
        const container = customContainer || document.getElementById('bytecode-content');
        if (!container) return;
        
        container.innerHTML = '';
        container.className = 'bytecode-container';
        
        const instructions = this.parseBytecode(bytecodeContent);
        instructions.forEach(instruction => {
            const lineElement = this.createInstructionLine(instruction);
            container.appendChild(lineElement);
        });
    }

    static parseBytecode(content) {
        const lines = content.split('\n').filter(line => line.trim() !== '');
        const instructions = [];
        
        lines.forEach(line => {
            const instruction = this.parseInstruction(line);
            if (instruction) {
                instructions.push(instruction);
            }
        });
        
        return instructions;
    }

    static parseInstruction(line) {
        // Формат: [] [0=LoadReferenceToLocalVar_a]
        const match = line.match(/\[\]\s*\[(\d+)=([^\]]+)\]/);
        if (!match) return null;
        
        const address = parseInt(match[1]);
        const fullInstruction = match[2];
        
        // Разделяем мнемонику и аргументы
        const parts = fullInstruction.split('_');
        let mnemonic = parts[0];
        let args = parts.slice(1).join('_');
        
        // Обработка специальных случаев
        if (mnemonic === 'Op') {
            mnemonic = 'Op_' + (parts[1] || '');
            args = parts.slice(2).join('_');
        }
        
        // Определяем категорию (упрощенная классификация)
        const category = this.classifyInstruction(mnemonic);
        
        // Генерируем подсказку
        const tooltip = this.generateTooltip(mnemonic, args, category);
        
        return {
            address,
            mnemonic,
            args: args || '',
            category,
            tooltip,
            fullInstruction
        };
    }

    static classifyInstruction(mnemonic) {
        // Упрощенная классификация с 4 категориями
        if (mnemonic.startsWith('Load') || mnemonic.startsWith('Push') || mnemonic.startsWith('Set')) {
            return 'memory'; // Операции с памятью
        }
        
        if (mnemonic.startsWith('Op') || mnemonic.startsWith('Call')) {
            return 'operation'; // Вычисления и вызовы
        }
        
        if (mnemonic.startsWith('Goto') || mnemonic.startsWith('Label') || mnemonic.startsWith('Jump')) {
            return 'control'; // Управление потоком
        }
        
        return 'other'; // Все остальное
    }

    static generateTooltip(mnemonic, args, category) {
        const categoryNames = {
            memory: 'Операция с памятью',
            operation: 'Вычисление или вызов',
            control: 'Управление потоком',
            other: 'Инструкция'
        };
        
        const descriptions = {
            'LoadReferenceToLocalVar': 'Загрузить ссылку на переменную',
            'LoadValueOfLocalVar': 'Загрузить значение переменной',
            'PushNumber': 'Поместить число в стек',
            'Set': 'Установить значение',
            'Op': 'Выполнить операцию',
            'Call': 'Вызов функции',
            'Label': 'Метка',
            'Goto': 'Переход к метке'
        };
        
        let description = descriptions[mnemonic] || `${categoryNames[category]}`;
        
        if (args) {
            description += `\nАргументы: ${args}`;
        }
        
        return description;
    }

    static createInstructionLine(instruction) {
        const lineElement = document.createElement('div');
        lineElement.className = 'bytecode-line';
        
        lineElement.innerHTML = `
            <div class="bytecode-address">${instruction.address}</div>
            <div class="bytecode-instruction">
                <div class="bytecode-mnemonic bytecode-${instruction.category}" title="${instruction.tooltip}">
                    ${instruction.mnemonic}
                    <div class="bytecode-tooltip">${instruction.tooltip}</div>
                </div>
                ${instruction.args ? `<div class="bytecode-args">${instruction.args}</div>` : ''}
            </div>
        `;
        
        return lineElement;
    }
}