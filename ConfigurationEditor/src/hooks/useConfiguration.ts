import { useConfigStore } from '@/stores/configStore';
import { ConfigRow } from '@/types/config';

export function useConfiguration() {
  const {
    updateRow,
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
    deleteRow: (rowId: string) => deleteRow(activeTab, rowId),
  };
}