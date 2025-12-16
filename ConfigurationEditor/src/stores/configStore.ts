import { create } from 'zustand';
import { devtools, persist } from 'zustand/middleware';
import { ConfigType, ConfigFile, ConfigRow } from '@/types/config';
import { parseParserConfig, parseLexerConfig, detectConfigType, formatToOriginal } from '@/utils/parser';
import { toast } from 'react-hot-toast';
import { arrayMove } from '@dnd-kit/sortable';

interface ConfigStore {
  // Данные
  parserConfig: ConfigFile | null;
  lexerConfig: ConfigFile | null;

  // UI состояние
  activeTab: ConfigType;
  searchQuery: string;

  // Действия
  setActiveTab: (tab: ConfigType) => void;
  setSearchQuery: (query: string) => void;
  loadFile: (file: File) => Promise<void>;
  updateRow: (configType: ConfigType, rowId: string, updates: Partial<ConfigRow>) => void;
  addRow: (configType: ConfigType) => void;
  deleteRow: (configType: ConfigType, rowId: string) => void;
  clearConfig: (configType: ConfigType) => void;
  exportConfig: (configType: ConfigType) => void;
  getFilteredRows: (configType: ConfigType) => ConfigRow[];
  getCurrentConfig: () => ConfigFile | null;
}

export const useConfigStore = create<ConfigStore>()(
  devtools(
    persist(
      (set, get) => ({
        parserConfig: null,
        lexerConfig: null,
        activeTab: ConfigType.PARSER,
        searchQuery: '',

        setActiveTab: (tab) => set({ activeTab: tab }),
        setSearchQuery: (query) => set({ searchQuery: query }),

        loadFile: async (file) => {
          try {
            const content = await file.text();
            const type = detectConfigType(content);

            let config: ConfigFile;
            if (type === ConfigType.PARSER) {
              config = parseParserConfig(content);
            } else {
              config = parseLexerConfig(content);
            }

            config.fileName = file.name;
            config.fileSize = file.size;

            if (type === ConfigType.PARSER) {
              set({
                parserConfig: config,
                activeTab: ConfigType.PARSER,
                searchQuery: '',
              });
              toast.success(`Загружен файл парсера: ${file.name} (${config.rows.length} строк)`);
            } else {
              set({
                lexerConfig: config,
                activeTab: ConfigType.LEXER,
                searchQuery: '',
              });
              toast.success(`Загружен файл лексера: ${file.name} (${config.rows.length} строк)`);
            }
          } catch (error) {
            console.error('Error loading file:', error);
            toast.error(`Ошибка загрузки: ${error instanceof Error ? error.message : 'Неизвестная ошибка'}`);
          }
        },

        updateRow: (configType, rowId, updates) => {
          const state = get();
          const config = configType === ConfigType.PARSER
            ? state.parserConfig
            : state.lexerConfig;

          if (!config) return;

          const updatedRows = config.rows.map(row =>
            row.id === rowId ? { ...row, ...updates } : row,
          );

          const updatedConfig = {
            ...config,
            rows: updatedRows,
            lastModified: new Date(),
          };

          if (configType === ConfigType.PARSER) {
            set({ parserConfig: updatedConfig });
          } else {
            set({ lexerConfig: updatedConfig });
          }

          toast.success('Строка обновлена');
        },

        addRow: (configType) => {
          const state = get();
          const config = configType === ConfigType.PARSER
            ? state.parserConfig
            : state.lexerConfig;

          if (!config) return;

          const newRow: ConfigRow = configType === ConfigType.PARSER ? {
            id: `parser-new-${Date.now()}`,
            priority: 0,
            type_full_name: 'NewModule.NewType',
            instance_hash: 0,
            ast_node_type: 'NewNode',
            lineNumber: config.rows.length + 1,
            originalLine: '',
          } : {
            id: `lexer-new-${Date.now()}`,
            priority: 0,
            encodedPattern: 'IA==',
            decodedPattern: ' ',
            lexeme_type: 'NewLexeme',
            ignore_flag: false,
            lineNumber: config.rows.length + 1,
            originalLine: '',
          };

          const updatedConfig = {
            ...config,
            rows: [...config.rows, newRow],
            lastModified: new Date(),
          };

          if (configType === ConfigType.PARSER) {
            set({ parserConfig: updatedConfig });
          } else {
            set({ lexerConfig: updatedConfig });
          }

          toast.success('Добавлена новая строка');
        },

        deleteRow: (configType, rowId) => {
          const state = get();
          const config = configType === ConfigType.PARSER
            ? state.parserConfig
            : state.lexerConfig;

          if (!config) return;

          const row = config.rows.find(r => r.id === rowId);
          if (!row) return;

          if (window.confirm(`Удалить строку ${row.lineNumber}?`)) {
            const updatedRows = config.rows.filter(row => row.id !== rowId);

            const updatedConfig = {
              ...config,
              rows: updatedRows,
              lastModified: new Date(),
            };

            if (configType === ConfigType.PARSER) {
              set({ parserConfig: updatedConfig });
            } else {
              set({ lexerConfig: updatedConfig });
            }

            toast.success('Строка удалена');
          }
        },

        clearConfig: (configType) => {
          if (window.confirm('Удалить текущую конфигурацию?')) {
            if (configType === ConfigType.PARSER) {
              set({ parserConfig: null });
              toast.success('Конфигурация парсера очищена');
            } else {
              set({ lexerConfig: null });
              toast.success('Конфигурация лексера очищена');
            }
          }
        },

        exportConfig: (configType) => {
          const state = get();
          const config = configType === ConfigType.PARSER
            ? state.parserConfig
            : state.lexerConfig;

          if (!config) {
            toast.error('Нет данных для экспорта');
            return;
          }

          try {
            const content = formatToOriginal(config);
            const blob = new Blob([content], { type: 'text/plain' });
            const url = URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = config.fileName || `config_export_${new Date().toISOString().slice(0, 10)}.txt`;
            document.body.appendChild(a);
            a.click();
            document.body.removeChild(a);
            URL.revokeObjectURL(url);
            toast.success('Конфигурация экспортирована');
          } catch (error) {
            console.error('Export error:', error);
            toast.error('Ошибка при экспорте');
          }
        },

        getFilteredRows: (configType) => {
          const state = get();
          const config = configType === ConfigType.PARSER
            ? state.parserConfig
            : state.lexerConfig;

          if (!config) return [];

          const query = state.searchQuery.toLowerCase().trim();
          if (!query) return config.rows;

          return config.rows.filter(row => {
            if (configType === ConfigType.PARSER) {
              const parserRow = row as any;
              return (
                parserRow.priority.toString().includes(query) ||
                parserRow.type_full_name.toLowerCase().includes(query) ||
                parserRow.ast_node_type.toLowerCase().includes(query) ||
                parserRow.module?.toLowerCase().includes(query) || false ||
                parserRow.instance_hash.toString().includes(query)
              );
            } else {
              const lexerRow = row as any;
              return (
                lexerRow.priority.toString().includes(query) ||
                lexerRow.decodedPattern.toLowerCase().includes(query) ||
                lexerRow.lexeme_type.toLowerCase().includes(query) ||
                lexerRow.encodedPattern.toLowerCase().includes(query) ||
                lexerRow.ignore_flag.toString().includes(query)
              );
            }
          });
        },

        getCurrentConfig: () => {
          const state = get();
          return state.activeTab === ConfigType.PARSER
            ? state.parserConfig
            : state.lexerConfig;
        },
      }),
      {
        name: 'config-editor-storage',
        partialize: (state) => ({
          parserConfig: state.parserConfig,
          lexerConfig: state.lexerConfig,
          activeTab: state.activeTab,
        }),
      },
    ),
  ),
);

