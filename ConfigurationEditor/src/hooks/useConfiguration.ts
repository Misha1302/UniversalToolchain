import { useConfigStore } from '@/stores/configStore';
import { ConfigType, ConfigRow } from '@/types/config';

export function useConfiguration() {
  const {
    updateRow,
    addRow,
    deleteRow,
    getCurrentConfig,
    getFilteredRows,
    activeTab,
  } = useConfigStore();

  const currentConfig = getCurrentConfig();
  const rows = getFilteredRows(activeTab);

  return {
    currentConfig,
    rows,
    activeTab,
    updateRow: (rowId: string, updates: Partial<ConfigRow>) =>
      updateRow(activeTab, rowId, updates),
    addRow: () => addRow(activeTab),
    deleteRow: (rowId: string) => deleteRow(activeTab, rowId),
  };
}