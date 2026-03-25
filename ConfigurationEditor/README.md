# ConfigurationEditor

## 1. What this tool is
`ConfigurationEditor` is a frontend utility for loading, editing, and exporting lexer/parser configuration text files used by the project.

## 2. Current stack
- React 19
- TypeScript
- Vite
- Zustand (state)
- AG Grid (table editing)
- dnd-kit
- react-dropzone (file input)
- MUI components/icons

## 3. Supported workflows in the current app
- Load `.txt` lexer or parser configuration files (file picker or drag-and-drop).
- Auto-detect configuration type and switch the active tab (`parser` / `lexer`).
- View loaded parser/lexer rows in a tabular grid.
- Edit loaded rows directly in the table cells.
- Filter rows via search.
- Export current tab back to text format.
- Clear current configuration state.
- Generate and load quick-start parser/lexer example content from UI buttons.

## 4. Development commands
From `ConfigurationEditor/`:

```bash
npm install
npm run dev
npm run build
npm run preview
npm run type-check
```

## 5. Status / limitations
- This is an internal project tool, not a hardened standalone product.
- UI text currently includes mixed-language labels/messages.
- Input acceptance is currently `.txt`-focused and tied to the project’s parser/lexer dump formats.
