import React from 'react';
import './EditorPanel.css';

interface EditorPanelProps {
  title?: string;
  children?: React.ReactNode;
}

const EditorPanel: React.FC<EditorPanelProps> = ({ title, children }) => {
  return (
    <div className="editor-panel">
      {title && (
        <div className="editor-header">
          <h3>{title}</h3>
        </div>
      )}
      <div className="editor-content">
        {children}
      </div>
    </div>
  );
};

export default EditorPanel;