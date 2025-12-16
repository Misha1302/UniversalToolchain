import React, { useCallback, useEffect, useState } from 'react';
import { DndContext, DragEndEvent, PointerSensor, useSensor, useSensors } from '@dnd-kit/core';
import { AgGridReact } from 'ag-grid-react';
import {
  AllCommunityModule,
  CellValueChangedEvent,
  ColDef,
  ICellRendererParams,
  ModuleRegistry,
} from 'ag-grid-community';
import 'ag-grid-community/styles/ag-grid.css';
import 'ag-grid-community/styles/ag-theme-alpine.css';
import { useConfigStore } from '@/stores/configStore';
import { ConfigType } from '@/types/config';
import { encodeBase64Pattern } from '@/utils/base64';
import './ConfigurationTable.css';

ModuleRegistry.registerModules([AllCommunityModule]);

interface ConfigurationTableProps {
  configType: ConfigType;
}

const ConfigurationTable: React.FC<ConfigurationTableProps> = ({ configType }) => {
  const {
    getFilteredRows,
    updateRow,
    addRow,
    reorderRow,
    searchQuery,
  } = useConfigStore();

  const [gridApi, setGridApi] = useState<any>(null);
  const [rows, setRows] = useState<any[]>([]);

  const sensors = useSensors(
    useSensor(PointerSensor, {
      activationConstraint: {
        distance: 8,
      },
    }),
  );

  useEffect(() => {
    const filteredRows = getFilteredRows(configType);
    setRows(filteredRows);
  }, [configType, getFilteredRows, searchQuery]);

  const handleDragEnd = useCallback((event: DragEndEvent) => {
    const { active, over } = event;

    if (over && active.id !== over.id) {
      const oldIndex = rows.findIndex(row => row.id === active.id);
      const newIndex = rows.findIndex(row => row.id === over.id);

      if (oldIndex !== -1 && newIndex !== -1) {
        reorderRow(configType, oldIndex, newIndex);
      }
    }
  }, [configType, rows, reorderRow]);

  // Добавить колонку с иконкой перетаскивания
  const dragColumn: ColDef = {
    headerName: '↕️',
    width: 60,
    sortable: false,
    filter: false,
    editable: false,
    cellRenderer: (params: ICellRendererParams) => (
      <div style={{ cursor: 'move', padding: '5px' }}>
        ⋮⋮
      </div>
    ),
  };

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
  ];
  // Обновить контейнер для поддержки DnD
  return (
    <DndContext sensors={sensors} onDragEnd={handleDragEnd}>
      <div className="configuration-table-container">
        <div className="table-actions">
          <button onClick={() => addRow(configType)} className="action-btn add-btn">
            + Добавить строку
          </button>
          <div className="table-stats">
            Показано: {rows.length} строк | Поиск: {searchQuery || '(нет)'}
          </div>
        </div>

        <div className="ag-theme-alpine" style={{ height: 'calc(100vh - 200px)', width: '100%' }}>
          <AgGridReact
            rowData={rows}
            columnDefs={configType === ConfigType.PARSER ? parserColumns : lexerColumns}
            defaultColDef={{
              resizable: true,
              sortable: true,
              filter: true,
            }}
            onGridReady={setGridApi}
            onCellValueChanged={(event: CellValueChangedEvent) => {
              const { data, colDef } = event;
              const field = colDef.field as string;

              if (field === 'decodedPattern' && 'decodedPattern' in data) {
                updateRow(configType, data.id, {
                  [field]: event.newValue,
                  encodedPattern: encodeBase64Pattern(event.newValue),
                });
              } else {
                updateRow(configType, data.id, { [field]: event.newValue });
              }
            }}
            rowDragManaged={true}
            animateRows={true}
            pagination={true}
            paginationPageSize={50}
          />
        </div>
      </div>
    </DndContext>
  );
};

export default ConfigurationTable;