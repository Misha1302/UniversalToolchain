import React from 'react';
import { ValidationError } from '@/types/config';
import { useValidation } from '@/hooks/useValidation';
import './ValidationPanel.css';

interface ValidationPanelProps {
  errors: ValidationError[];
  onErrorClick?: (rowId: string) => void;
}

const ValidationPanel: React.FC<ValidationPanelProps> = ({ errors, onErrorClick }) => {
  const { getErrorSummary } = useValidation();
  const summary = getErrorSummary(errors);
  
  if (errors.length === 0) {
    return (
      <div className="validation-panel valid">
        <div className="validation-status">
          <span className="status-icon">✅</span>
          <span className="status-text">Все проверки пройдены успешно</span>
        </div>
      </div>
    );
  }
  
  const handleErrorClick = (rowId: string) => {
    if (onErrorClick) {
      onErrorClick(rowId);
    }
  };
  
  return (
    <div className={`validation-panel ${summary.hasErrors ? 'has-errors' : 'has-warnings'}`}>
      <div className="validation-header">
        <div className="validation-summary">
          <span className="summary-icon">
            {summary.hasErrors ? '❌' : '⚠️'}
          </span>
          <span className="summary-text">
            Найдено {summary.errors} ошибок и {summary.warnings} предупреждений
          </span>
        </div>
      </div>
      
      <div className="validation-errors">
        {errors.map((error, index) => (
          <div 
            key={index} 
            className={`validation-error ${error.severity}`}
            onClick={() => handleErrorClick(error.rowId)}
            style={{ cursor: onErrorClick ? 'pointer' : 'default' }}
          >
            <div className="error-header">
              <span className="error-type">
                {error.severity === 'error' ? 'Ошибка' : 'Предупреждение'}
              </span>
              <span className="error-field">Поле: {error.field}</span>
              <span className="error-row">Строка: {error.rowId.split('-')[1] || '?'}</span>
            </div>
            <div className="error-message">{error.message}</div>
          </div>
        ))}
      </div>
    </div>
  );
};

export default ValidationPanel;
