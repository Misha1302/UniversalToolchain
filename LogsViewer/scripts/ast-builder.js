// Модуль для построения AST дерева
class ASTBuilder {
    constructor() {
        this.zoomLevel = 1;
    }

    display(astContent) {
        const astData = LogParser.parseAST(astContent);
        this.renderTree(astData);
        this.setupEventListeners();
    }

    renderTree(astData) {
        const astTree = document.getElementById('ast-tree');

        if (!astTree) return;

        astTree.innerHTML = '';

        const treeElement = this.createTreeNode(astData, 0);
        astTree.appendChild(treeElement);

        this.addConnectors(astTree);
        this.applyZoom();
    }

    createTreeNode(node, depth) {
        const nodeElement = document.createElement('div');
        nodeElement.className = 'node';

        // Добавляем класс уровня для стилизации
        const levelClass = `level-${Math.min(depth, 5)}`;
        nodeElement.classList.add(levelClass);

        // Добавляем data-атрибут для информации об уровне
        nodeElement.setAttribute('data-level', depth);

        const contentElement = this.createNodeContent(node, depth);
        nodeElement.appendChild(contentElement);

        if (node.children && node.children.length > 0) {
            const childrenContainer = this.createChildrenContainer(node.children, depth + 1);
            nodeElement.appendChild(childrenContainer);
        }

        return nodeElement;
    }

    createNodeContent(node, depth) {
        const contentElement = document.createElement('div');
        contentElement.className = 'node-content';

        const typeElement = document.createElement('div');
        typeElement.className = 'node-type';
        typeElement.textContent = node.type;
        contentElement.appendChild(typeElement);

        if (node.value) {
            const valueElement = document.createElement('div');
            valueElement.className = 'node-value';
            valueElement.textContent = node.value;
            contentElement.appendChild(valueElement);
        }

        if (node.position) {
            const positionElement = document.createElement('div');
            positionElement.className = 'node-position';
            positionElement.textContent = `at ${node.position}`;
            contentElement.appendChild(positionElement);
        }

        // Добавляем текстовый индикатор уровня
        const levelIndicator = document.createElement('div');
        levelIndicator.className = 'level-indicator';
        levelIndicator.textContent = `Ур. ${depth}`;
        contentElement.appendChild(levelIndicator);

        return contentElement;
    }

    createChildrenContainer(children, depth) {
        const childrenContainer = document.createElement('div');
        childrenContainer.className = 'children';

        children.forEach(child => {
            const childElement = this.createTreeNode(child, depth);
            childrenContainer.appendChild(childElement);
        });

        return childrenContainer;
    }

    addConnectors(container) {
        const nodesWithChildren = container.querySelectorAll('.node:has(.children)');

        nodesWithChildren.forEach(node => {
            const childrenContainer = node.querySelector('.children');
            const children = childrenContainer.querySelectorAll('.node');

            if (children.length === 0) return;

            this.addVerticalConnectors(node, childrenContainer, children);
        });
    }

    addVerticalConnectors(parent, childrenContainer, children) {
        const parentRect = parent.getBoundingClientRect();
        const containerRect = childrenContainer.getBoundingClientRect();

        // Вертикальная линия от родителя к контейнеру детей
        const verticalLine = document.createElement('div');
        verticalLine.className = 'connector vertical';
        verticalLine.style.height = '15px';
        verticalLine.style.top = `${parentRect.height}px`;
        verticalLine.style.left = '50%';
        verticalLine.style.transform = 'translateX(-50%)';
        parent.appendChild(verticalLine);

        // Горизонтальная линия над детьми
        const horizontalLine = document.createElement('div');
        horizontalLine.className = 'connector horizontal';
        horizontalLine.style.width = `${containerRect.width}px`;
        horizontalLine.style.height = '1px';
        horizontalLine.style.top = `${parentRect.height + 15}px`;
        horizontalLine.style.left = '50%';
        horizontalLine.style.transform = 'translateX(-50%)';
        parent.appendChild(horizontalLine);

        // Вертикальные линии к каждому ребенку
        children.forEach((child, index) => {
            const childRect = child.getBoundingClientRect();
            const containerRect = childrenContainer.getBoundingClientRect();

            const line = document.createElement('div');
            line.className = 'connector vertical';
            line.style.height = '15px';
            line.style.top = `${parentRect.height + 15}px`;
            line.style.left = `${childRect.left - containerRect.left + childRect.width / 2}px`;
            line.style.transform = 'translateX(-50%)';
            childrenContainer.appendChild(line);
        });
    }

    setupEventListeners() {
        document.getElementById('zoom-in').addEventListener('click', () => this.zoomIn());
        document.getElementById('zoom-out').addEventListener('click', () => this.zoomOut());
        document.getElementById('reset-view').addEventListener('click', () => this.resetView());
    }

    zoomIn() {
        this.zoomLevel = Math.min(this.zoomLevel + 0.1, 2);
        this.applyZoom();
    }

    zoomOut() {
        this.zoomLevel = Math.max(this.zoomLevel - 0.1, 0.5);
        this.applyZoom();
    }

    resetView() {
        this.zoomLevel = 1;
        this.applyZoom();
    }

    applyZoom() {
        const astTree = document.getElementById('ast-tree');
        if (astTree) {
            astTree.style.transform = `scale(${this.zoomLevel})`;
        }
    }
}