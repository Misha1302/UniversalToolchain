import React, { useEffect, useState } from 'react';
import { Toaster } from 'react-hot-toast';
import { useConfigStore } from '@/stores/configStore';
import { ConfigType } from '@/types/config';
import Header from '@/components/Header/Header';
import FileUploader from '@/components/FileUploader/FileUploader';
import ConfigurationTable from '@/components/ConfigurationTable/ConfigurationTable';
import './App.css';

function App() {
  const {
    parserConfig,
    lexerConfig,
    activeTab,
    getCurrentConfig
  } = useConfigStore();

  const [isInitialized, setIsInitialized] = useState(false);

  useEffect(() => {
    setIsInitialized(true);
  }, []);

  const currentConfig = getCurrentConfig();
  const hasConfig = parserConfig || lexerConfig;

  useEffect(() => {
    console.log('App state:', {
      hasConfig,
      parserConfig: parserConfig ? {
        fileName: parserConfig.fileName,
        rowsCount: parserConfig.rows.length,
        firstRow: parserConfig.rows[0]
      } : null,
      lexerConfig: lexerConfig ? {
        fileName: lexerConfig.fileName,
        rowsCount: lexerConfig.rows.length,
        firstRow: lexerConfig.rows[0]
      } : null,
      activeTab,
      currentConfig: currentConfig ? {
        fileName: currentConfig.fileName,
        rowsCount: currentConfig.rows.length
      } : null
    });
  }, [hasConfig, parserConfig, lexerConfig, activeTab, currentConfig]);

  if (!isInitialized) {
    return (
      <div className="loading-screen">
        <div className="loading-spinner"></div>
        <p>Загрузка приложения...</p>
      </div>
    );
  }

  return (
    <div className="app">
      <Toaster
        position="top-right"
        toastOptions={{
          duration: 3000,
          style: {
            background: '#363636',
            color: '#fff',
            fontSize: '14px',
          },
          success: {
            duration: 2000,
            style: {
              background: '#10b981',
            },
          },
          error: {
            duration: 4000,
            style: {
              background: '#ef4444',
            },
          },
        }}
      />

      <Header />

      <main className="main-content">
        {!hasConfig ? (
          <div className="welcome-screen">
            <div className="welcome-header">
              <h1>⚙️ Конфигурационный редактор</h1>
              <p className="subtitle">Удобный инструмент для редактирования конфигураций лексера и парсера</p>
            </div>

            <div className="upload-section">
              <h2>📁 Начните с загрузки файла</h2>
              <FileUploader />
              <div className="file-examples">
                <p>Поддерживаемые форматы:</p>
                <div className="example">
                  <h4>ParserConfiguration.txt</h4>
                  <pre>{`-100000.00|ScopesModule.ScopesCreator|0|Scope
-1000.00|CSharpInteropModule.CSharpFunctionCallsNodeCreator|0|CSharpFunctionCall
-100.00|ConditionsModule.BooleanNodeCreator|0|True`}</pre>
                </div>
                <div className="example">
                  <h4>LexerConfiguration.txt</h4>
                  <pre>{`100.00|W0BhLXpBLVpfXVthLXpBLVowLTlfXSo=|Identifier|False
0.00|IA==|Space|True
0.00|XG4=|NewLine|True`}</pre>
                </div>
              </div>
            </div>

            <div className="quick-start">
              <h3>🚀 Быстрый старт:</h3>
              <div className="quick-buttons">
                <button
                  className="quick-btn"
                  onClick={async () => {
                    const example = `# Parser Configuration Example
-100000.00|ScopesModule.ScopesCreator|0|Scope
-1000.00|CSharpInteropModule.CSharpFunctionCallsNodeCreator|0|CSharpFunctionCall
-100.00|ConditionsModule.BooleanNodeCreator|0|True
-100.00|ConditionsModule.BooleanNodeCreator|1|False
-20.00|ConditionsModule.ComparisonNodeCreator|0|Equal`;
                    const blob = new Blob([example], { type: 'text/plain' });
                    const file = new File([blob], 'ParserExample.txt', { type: 'text/plain' });
                    await useConfigStore.getState().loadFile(file);
                  }}
                >
                  📋 Создать пример парсера
                </button>
                <button
                  className="quick-btn"
                  onClick={async () => {
                    const example = `# Lexer Configuration Example
100.00|W0BhLXpBLVpfXVthLXpBLVowLTlfXSo=|Identifier|False
0.00|IA==|Space|True
0.00|XG4=|NewLine|True
0.00|XCg=|OpenPar|False
0.00|XCk=|ClosePar|False`;
                    const blob = new Blob([example], { type: 'text/plain' });
                    const file = new File([blob], 'LexerExample.txt', { type: 'text/plain' });
                    await useConfigStore.getState().loadFile(file);
                  }}
                >
                  📋 Создать пример лексера
                </button>
              </div>
            </div>
          </div>
        ) : (
          <div className="editor-screen">
            {currentConfig ? (
              <>
                <div className="editor-header">
                  <h2>
                    {activeTab === ConfigType.PARSER ? '📄' : '🔤'}
                    {currentConfig.fileName}
                    <span className="file-stats">
                      ({currentConfig.rows.length} строк, {currentConfig.fileSize} байт)
                    </span>
                  </h2>
                  <div className="editor-actions">
                    <button
                      className="action-btn"
                      onClick={() => useConfigStore.getState().addRow(activeTab)}
                    >
                      + Добавить строку
                    </button>
                    <button
                      className="action-btn export"
                      onClick={() => useConfigStore.getState().exportConfig(activeTab)}
                    >
                      💾 Экспорт
                    </button>
                  </div>
                </div>
                <ConfigurationTable configType={activeTab} />
              </>
            ) : (
              <div className="no-config">
                <p>Конфигурация не загружена</p>
              </div>
            )}
          </div>
        )}
      </main>

      <footer className="footer">
        <div className="footer-content">
          <p>⚡ Конфигурационный редактор v1.0.0</p>
          <div className="footer-stats">
            {hasConfig && (
              <>
                <span>Парсер: {parserConfig?.rows.length || 0} строк</span>
                <span>Лексер: {lexerConfig?.rows.length || 0} строк</span>
                <span>Активная вкладка: {activeTab}</span>
              </>
            )}
          </div>
        </div>
      </footer>
    </div>
  );
}

export default App;
