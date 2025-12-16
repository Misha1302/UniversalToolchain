import React, { useCallback } from 'react';
import { useDropzone } from 'react-dropzone';
import { useConfigStore } from '@/stores/configStore';
import { toast } from 'react-hot-toast';
import './FileUploader.css';

interface FileUploaderProps {
  onUploadStart?: () => void;
  onUploadEnd?: () => void;
}

const FileUploader: React.FC<FileUploaderProps> = ({ 
  onUploadStart, 
  onUploadEnd 
}) => {
  const { loadFile } = useConfigStore();
  
  const onDrop = useCallback(async (acceptedFiles: File[]) => {
    if (acceptedFiles.length === 0) return;
    
    const file = acceptedFiles[0];
    
    // Проверка размера (2MB)
    if (file.size > 2 * 1024 * 1024) {
      toast.error('Файл слишком большой (максимум 2MB)');
      return;
    }
    
    try {
      onUploadStart?.();
      await loadFile(file);
      toast.success(`Файл "${file.name}" успешно загружен`);
    } catch (error) {
      console.error('Ошибка загрузки файла:', error);
      toast.error(`Ошибка загрузки: ${error instanceof Error ? error.message : 'Неизвестная ошибка'}`);
    } finally {
      onUploadEnd?.();
    }
  }, [loadFile, onUploadStart, onUploadEnd]);
  
  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    onDrop,
    accept: {
      'text/plain': ['.txt'],
    },
    multiple: false,
    maxSize: 2 * 1024 * 1024, // 2MB
  });
  
  return (
    <div
      {...getRootProps()}
      className={`file-uploader ${isDragActive ? 'drag-active' : ''}`}
    >
      <input {...getInputProps()} />
      
      <div className="upload-content">
        <div className="upload-icon">📁</div>
        <h3>Перетащите файл сюда или кликните для выбора</h3>
        <p>Поддерживаются файлы .txt до 2MB</p>
        <div className="file-types">
          <span className="file-type">ParserConfiguration.txt</span>
          <span className="file-type">LexerConfiguration.txt</span>
        </div>
      </div>
    </div>
  );
};

export default FileUploader;
