import React, { useCallback, useState, useEffect } from 'react';
import { AgGridReact } from 'ag-grid-react';
import { ColDef, ICellRendererParams, CellValueChangedEvent } from 'ag-grid-community';
import { ModuleRegistry, AllCommunityModule } from 'ag-grid-community';
import 'ag-grid-community/styles/ag-grid.css';
import 'ag-grid-community/styles/ag-theme-alpine.css';
import { useConfigStore } from '@/stores/configStore';
import { ConfigType, ParserConfigRow, LexerConfigRow } from '@/types/config';
import { encodeBase64Pattern } from '@/utils/base64';
import './ConfigurationTable.css';

// Регистрируем модули AG Grid
ModuleRegistry.registerModules([AllCommunityModule]);

interface ConfigurationTableProps {
  configType: ConfigType;
}

const ConfigurationTable: React.FC<ConfigurationTableProps> = ({ configType }) => {
  const {
    getFilteredRows,
    updateRow,
    deleteRow,
    addRow,
    exportConfig,
    searchQuery
  } = useConfigStore();

  const [gridApi, setGridApi] = useState<any>(null);
  const [rows, setRows] = useState<any[]>([]);

  useEffect(() => {
    const filteredRows = getFilteredRows(configType);
    console.log('ConfigurationTable - configType:', configType);
    console.log('ConfigurationTable - rows count:', filteredRows.length);
    console.log('ConfigurationTable - first row:', filteredRows[0]);
    console.log('ConfigurationTable - searchQuery:', searchQuery);
    setRows(filteredRows);
  }, [configType, getFilteredRows, searchQuery]);

  // Колонки для парсера
  const parserColumns: ColDef[] = [
    {
      headerName: 'Приоритет',
      field: 'priority',
      width: 120,
      editable: true,
      cellEditor: 'agNumberCellEditor',
      cellEditorParams: {
        min: -1000000,
        max: 1000000,
        precision: 2,
      },
      sortable: true,
      filter: 'agNumberColumnFilter',
    },
    {
      headerName: 'Тип',
      field: 'type_full_name',
      width: 300,
      editable: true,
      sortable: true,
      filter: 'agTextColumnFilter',
    },
    {
      headerName: 'Хэш',
      field: 'instance_hash',
      width: 100,
      editable: true,
      cellEditor: 'agNumberCellEditor',
      sortable: true,
      filter: 'agNumberColumnFilter',
    },
    {
      headerName: 'AST-узел',
      field: 'ast_node_type',
      width: 200,
      editable: true,
      sortable: true,
      filter: 'agTextColumnFilter',
    },
    {
      headerName: 'Модуль',
      field: 'module',
      width: 150,
      sortable: true,
      filter: 'agTextColumnFilter',
    },
    {
      headerName: 'Действия',
      width: 120,
      cellRenderer: (params: ICellRendererParams) => (
        <button
          className="delete-btn"
          onClick={() => deleteRow(configType, params.data.id)}
        >
          Удалить
        </button>
      ),
    },
  ];

  // Колонки для лексера
  const lexerColumns: ColDef[] = [
    {
      headerName: 'Приоритет',
      field: 'priority',
      width: 120,
      editable: true,
      cellEditor: 'agNumberCellEditor',
      cellEditorParams: {
        min: -1000000,
        max: 1000000,
        precision: 2,
      },
      sortable: true,
      filter: 'agNumberColumnFilter',
    },
    {
      headerName: 'Паттерн (декодированный)',
      field: 'decodedPattern',
      width: 300,
      editable: true,
      sortable: true,
      filter: 'agTextColumnFilter',
      cellRenderer: (params: ICellRendererParams) => (
        <div className="pattern-cell">
          <span className="pattern-text">{params.value}</span>
          {params.data.regexError && (
            <span className="regex-error" title={params.data.regexError}>⚠</span>
          )}
        </div>
      ),
    },
    {
      headerName: 'Тип лексемы',
      field: 'lexeme_type',
      width: 150,
      editable: true,
      sortable: true,
      filter: 'agTextColumnFilter',
    },
    {
      headerName: 'Игнорировать',
      field: 'ignore_flag',
      width: 120,
      editable: true,
      cellEditor: 'agCheckboxCellEditor',
      cellRenderer: (params: ICellRendererParams) => (
        <input
          type="checkbox"
          checked={params.value}
          readOnly
        />
      ),
    },
    {
      headerName: 'Base64',
      field: 'encodedPattern',
      width: 200,
      cellRenderer: (params: ICellRendererParams) => (
        <code className="base64-code">{params.value}</code>
      ),
    },
    {
      headerName: 'Действия',
      width: 120,
      cellRenderer: (params: ICellRendererParams) => (
        <button
          className="delete-btn"
          onClick={() => deleteRow(configType, params.data.id)}
        >
          Удалить
        </button>
      ),
    },
  ];

  const columns = configType === ConfigType.PARSER ? parserColumns : lexerColumns;

  const handleCellValueChanged = useCallback((event: CellValueChangedEvent) => {
    const { data, colDef } = event;
    const field = colDef.field as string;

    // Проверяем, является ли это поле decodedPattern и есть ли оно в данных
    if (field === 'decodedPattern' && 'decodedPattern' in data) {
      // Это лексер строка
      const newValue = event.newValue;
      updateRow(configType, data.id, {
        [field]: newValue,
        encodedPattern: encodeBase64Pattern(newValue),
      });
    } else {
      // Для всех остальных полей
      updateRow(configType, data.id, { [field]: event.newValue });
    }
  }, [configType, updateRow]);

  const handleAddRow = () => {
    addRow(configType);
    setTimeout(() => {
      if (gridApi) {
        gridApi.ensureIndexVisible(rows.length, 'bottom');
      }
    }, 0);
  };

  const handleExport = () => {
    exportConfig(configType);
  };

  const onGridReady = (params: any) => {
    setGridApi(params.api);
  };


  if (rows.length === 0) {
    return (
      <div className="empty-table">
        <p>Нет данных для отображения</p>
        <button onClick={handleAddRow} className="action-btn add-btn">
          + Добавить строку
        </button>
        <p style={{ fontSize: '0.9rem', color: '#718096', marginTop: '1rem' }}>
          Загрузите файл или создайте пример через меню выше
        </p>
      </div>
    );
  }

  return (
    <div className="configuration-table-container">
      <div className="table-actions">
        <button onClick={handleAddRow} className="action-btn add-btn">
          + Добавить строку
        </button>
        <button onClick={handleExport} className="action-btn export-btn">
          📥 Экспорт
        </button>
        <div className="table-stats">
          Показано: {rows.length} строк | Поиск: {searchQuery || '(нет)'}
        </div>
      </div>

      <div className="ag-theme-alpine" style={{ height: 'calc(100vh - 200px)', width: '100%' }}>
        <AgGridReact
          rowData={rows}
          columnDefs={columns}
          defaultColDef={{
            resizable: true,
            sortable: true,
            filter: true,
            editable: false, // временно отключаем редактирование для дебага
          }}
          onGridReady={onGridReady}
          onCellValueChanged={handleCellValueChanged}
          rowSelection="multiple"
          animateRows={true}
          pagination={true}
          paginationPageSize={50}
          suppressMovableColumns={true}
          suppressDragLeaveHidesColumns={true}
        />
      </div>
    </div>
  );
};

export default ConfigurationTable;