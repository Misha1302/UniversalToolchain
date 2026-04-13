class CILBuilder {
    constructor() {
        this.methods = [];
        this.currentCILContent = '';
    }

    display(cilContent) {
        this.currentCILContent = cilContent;
        const container = document.getElementById('dotnet-content');
        if (!container) return;

        container.innerHTML = '';
        container.className = 'cil-container';

        this.parseCIL(cilContent);
        this.renderMethods(container);
        this.setupEventListeners();
    }

    parseCIL(content) {
        const lines = content.split('\n');
        this.methods = [];
        let currentMethod = null;
        let instructionCount = 0;
        let pendingStackTypes = '';

        for (let i = 0; i < lines.length; i++) {
            const line = lines[i];
            const trimmedLine = line.trim();

            if (trimmedLine === '') continue;

            if (trimmedLine.endsWith(':') && !line.startsWith(' ')) {
                if (currentMethod) {
                    this.methods.push(currentMethod);
                }
                currentMethod = {
                    name: trimmedLine.slice(0, -1).trim(),
                    instructions: [],
                    startAddress: instructionCount
                };
                pendingStackTypes = '';
            }
            else if (currentMethod && line.startsWith('        ') && trimmedLine !== '') {
                if (trimmedLine.startsWith('//')) {
                    pendingStackTypes = trimmedLine.substring(2).trim().replace(/[\[\]]/g, '');
                    continue;
                }

                let instructionLine = trimmedLine;
                let stackTypes = pendingStackTypes;
                pendingStackTypes = '';

                const commentIndex = instructionLine.indexOf('//');
                if (commentIndex !== -1) {
                    const commentContent = instructionLine.substring(commentIndex + 2).trim();
                    if (commentContent.startsWith('[') && commentContent.endsWith(']')) {
                        stackTypes = commentContent.replace(/[\[\]]/g, '');
                    }
                    instructionLine = instructionLine.substring(0, commentIndex).trim();
                }

                const instruction = this.parseInstruction(instructionLine, instructionCount, stackTypes);
                if (instruction) {
                    currentMethod.instructions.push(instruction);
                    instructionCount++;
                }
            }
        }

        if (currentMethod) {
            this.methods.push(currentMethod);
        }
    }

    parseInstruction(line, address, stackTypes) {
        const instructionPart = line;

        const parts = instructionPart.split(/\s+/);
        if (parts.length === 0) return null;

        const opcode = parts[0];
        const operand = parts.slice(1).join(' ').replace(/,/g, ', ');

        const bytecodeLinks = this.detectBytecodeLinks(opcode, operand);

        return {
            address,
            opcode,
            operand: operand || '',
            stackTypes,
            original: line.trim(),
            bytecodeLinks
        };
    }

    detectBytecodeLinks(opcode, operand) {
        const links = [];

        const patterns = [
            {
                test: (op, opnd) => op === 'ldstr' && opnd.includes("'a'"),
                bytecode: 'LoadReferenceToLocalVar_a'
            },
            {
                test: (op, opnd) => op === 'ldstr' && opnd.includes("'b'"),
                bytecode: 'LoadReferenceToLocalVar_b'
            },
            {
                test: (op, opnd) => op === 'ldc.r8' && opnd.includes('-5'),
                bytecode: 'PushNumber_-5'
            },
            {
                test: (op, opnd) => op === 'ldc.r8' && opnd.includes('7'),
                bytecode: 'PushNumber_7'
            },
            {
                test: (op, opnd) => op === 'ldc.r8' && opnd.includes('1'),
                bytecode: 'PushNumber_1'
            },
            {
                test: (op, opnd) => op === 'call' && opnd.includes('VariablesContainer.GetRef'),
                bytecode: 'LoadReferenceToLocalVar'
            },
            {
                test: (op, opnd) => op === 'call' && opnd.includes('VariablesContainer.Get'),
                bytecode: 'LoadValueOfLocalVar'
            },
            {
                test: (op, opnd) => op === 'callvirt' && opnd.includes('SetValue'),
                bytecode: 'Set'
            },
            {
                test: (op, opnd) => op === 'call' && opnd.includes('RealNumberImpl.Add'),
                bytecode: 'Op_+'
            },
            {
                test: (op, opnd) => op === 'call' && opnd.includes('Main.Print'),
                bytecode: 'Call_Main.Print'
            },
            {
                test: (op, opnd) => op === 'newobj' && opnd.includes('RealNumberImpl.ctor'),
                bytecode: /PushNumber_/
            }
        ];

        patterns.forEach(pattern => {
            if (pattern.test(opcode, operand)) {
                links.push(pattern.bytecode);
            }
        });

        return links;
    }

    renderMethods(container) {
        this.methods.forEach(method => {
            const methodElement = this.createMethodElement(method);
            container.appendChild(methodElement);
        });
    }

    createMethodElement(method) {
        const methodElement = document.createElement('div');
        methodElement.className = 'cil-method';

        const header = document.createElement('div');
        header.className = 'cil-method-header';
        header.innerHTML = `
            <div class="cil-method-name">${method.name}</div>
            <button class="cil-method-toggle">−</button>
        `;

        const body = document.createElement('div');
        body.className = 'cil-method-body';

        method.instructions.forEach(instruction => {
            const instructionElement = this.createInstructionElement(instruction);
            body.appendChild(instructionElement);
        });

        methodElement.appendChild(header);
        methodElement.appendChild(body);

        header.addEventListener('click', () => {
            const isCollapsed = methodElement.classList.contains('collapsed');
            methodElement.classList.toggle('collapsed');
            header.querySelector('.cil-method-toggle').textContent = isCollapsed ? '−' : '+';
        });

        return methodElement;
    }

    createInstructionElement(instruction) {
        const element = document.createElement('div');
        element.className = 'cil-instruction';
        element.dataset.address = instruction.address;
        element.dataset.bytecodeLinks = instruction.bytecodeLinks.join(',');

        if (instruction.bytecodeLinks.length > 0) {
            element.style.cursor = 'pointer';
            element.title = `Linked to: ${instruction.bytecodeLinks.join(', ')}`;
        }

        let formattedOperand = instruction.operand;
        if (formattedOperand.length > 70) {
            formattedOperand = formattedOperand.substring(0, 70) + '...';
        }

        let stackTypesHTML = '';
        if (instruction.stackTypes) {
            const types = instruction.stackTypes.split(',').map(type => type.trim());
            stackTypesHTML = `
                <div class="cil-stack">
                    ${types.map(type => `<span class="stack-type">${type}</span>`).join('')}
                </div>
            `;
        }

        element.innerHTML = `
            <div class="cil-address">${instruction.address}</div>
            <div class="cil-opcode">${instruction.opcode}</div>
            <div class="cil-operand">${formattedOperand}</div>
            ${stackTypesHTML}
        `;

        return element;
    }

    setupEventListeners() {
        document.addEventListener('click', (e) => {
            if (!e.target.closest('.cil-instruction')) {
                this.resetHighlighting();
            }
        });
    }

    highlightBytecodeInstructions(bytecodePatterns) {
        this.resetHighlighting();

        document.querySelectorAll('.cil-instruction').forEach(element => {
            const links = element.dataset.bytecodeLinks.split(',');
            const hasMatch = links.some(link =>
                bytecodePatterns.some(pattern =>
                    link.includes(pattern) || pattern.includes(link)
                )
            );
            if (hasMatch) {
                element.classList.add('related');
            }
        });

        const bytecodeSection = document.getElementById('bytecode');
        if (bytecodeSection) {
            document.querySelectorAll('.nav-btn').forEach(btn => btn.classList.remove('active'));
            document.querySelectorAll('.content-section').forEach(section => section.classList.remove('active'));

            document.querySelector('[data-section="bytecode"]').classList.add('active');
            bytecodeSection.classList.add('active');

            const bytecodeInstructions = bytecodeSection.querySelectorAll('.bytecode-mnemonic');
            bytecodeInstructions.forEach(el => {
                const mnemonic = el.textContent.trim();
                const isMatch = bytecodePatterns.some(pattern => {
                    if (pattern instanceof RegExp) {
                        return pattern.test(mnemonic);
                    }
                    return mnemonic.includes(pattern);
                });

                if (isMatch) {
                    el.scrollIntoView({behavior: 'smooth', block: 'center'});
                    el.classList.add('highlighted');

                    setTimeout(() => {
                        el.classList.remove('highlighted');
                    }, 3000);
                }
            });
        }
    }

    resetHighlighting() {
        document.querySelectorAll('.cil-instruction.related').forEach(el => {
            el.classList.remove('related');
        });

        document.querySelectorAll('.bytecode-mnemonic.highlighted').forEach(el => {
            el.classList.remove('highlighted');
        });
    }
}
