import React, { useRef } from 'react';
import { ConfigType } from '@/types/config';
import { useConfigStore } from '@/stores/configStore';
import './Header.css';

const Header: React.FC = () => {
  const { activeTab, setActiveTab, parserConfig, lexerConfig, exportConfig, clearConfig } = useConfigStore();
  const fileInputRef = useRef<HTMLInputElement>(null);
  
  const handleFileSelect = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (file) {
      useConfigStore.getState().loadFile(file);
    }
  };
  
  const handleExport = () => {
    exportConfig(activeTab);
  };
  
  const handleClear = () => {
    if (window.confirm('Удалить текущую конфигурацию?')) {
      clearConfig(activeTab);
    }
  };
  
  const handleExampleClick = async (type: ConfigType) => {
    const exampleContent = type === ConfigType.PARSER 
      ? `# Parser Configuration Dump
# Generated: 2025-12-15 19:56:18
# Format: <priority>|<type_full_name>|<instance_hash>|<ast_node_type>

-100000.00|ScopesModule.ScopesCreator|0|Scope
-1000.00|CSharpInteropModule.CSharpFunctionCallsNodeCreator|0|CSharpFunctionCall
-100.00|ConditionsModule.BooleanNodeCreator|0|True
-100.00|ConditionsModule.BooleanNodeCreator|1|False
-20.00|ConditionsModule.ComparisonNodeCreator|0|Equal`
      : `# Lexer Configuration Dump
# Generated: 2025-12-15 19:38:19
# Format: <priority>|<base64_encoded_pattern>|<lexeme_type>|<ignore_flag>

100.00|W0BhLXpBLVpfXVthLXpBLVowLTlfXSo=|Identifier|False
0.00|IA==|Space|True
0.00|XG4=|NewLine|True
0.00|XCg=|OpenPar|False
0.00|XCk=|ClosePar|False`;
    
    const blob = new Blob([exampleContent], { type: 'text/plain' });
    const file = new File([blob], `${type === ConfigType.PARSER ? 'Parser' : 'Lexer'}Configuration.txt`, { type: 'text/plain' });
    await useConfigStore.getState().loadFile(file);
  };
  
  return (
    <header className="header">
      <div className="header-left">
        <h1 className="logo">⚙️ Config Editor</h1>
      </div>
      
      <div className="header-center">
        <div className="tabs">
          <button
            className={`tab ${activeTab === ConfigType.PARSER ? 'active' : ''}`}
            onClick={() => setActiveTab(ConfigType.PARSER)}
          >
            📄 Парсер
            {parserConfig && <span className="badge">{parserConfig.rows.length}</span>}
          </button>
          <button
            className={`tab ${activeTab === ConfigType.LEXER ? 'active' : ''}`}
            onClick={() => setActiveTab(ConfigType.LEXER)}
          >
            🔤 Лексер
            {lexerConfig && <span className="badge">{lexerConfig.rows.length}</span>}
          </button>
        </div>
        
        <div className="search-box">
          <input
            type="text"
            placeholder="Поиск..."
            value={useConfigStore.getState().searchQuery}
            onChange={(e) => useConfigStore.getState().setSearchQuery(e.target.value)}
            className="search-input"
          />
        </div>
      </div>
      
      <div className="header-right">
        <input
          type="file"
          ref={fileInputRef}
          onChange={handleFileSelect}
          accept=".txt"
          style={{ display: 'none' }}
        />
        
        <div className="example-buttons">
          <button 
            className="btn btn-example"
            onClick={() => handleExampleClick(ConfigType.PARSER)}
            title="Загрузить пример парсера"
          >
            📋 Парсер
          </button>
          <button 
            className="btn btn-example"
            onClick={() => handleExampleClick(ConfigType.LEXER)}
            title="Загрузить пример лексера"
          >
            📋 Лексер
          </button>
        </div>
        
        <button 
          className="btn btn-secondary"
          onClick={() => fileInputRef.current?.click()}
        >
          📁 Загрузить
        </button>
        
        {(parserConfig || lexerConfig) && (
          <>
            <button className="btn btn-primary" onClick={handleExport}>
              💾 Экспорт
            </button>
            <button className="btn btn-danger" onClick={handleClear}>
              🗑️ Очистить
            </button>
          </>
        )}
      </div>
    </header>
  );
};

export default Header;
