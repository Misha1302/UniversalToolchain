import { ConfigType, ConfigFile, ParserConfigRow, LexerConfigRow } from '@/types/config';
import { decodeBase64Pattern, encodeBase64Pattern } from './base64';

export function detectConfigType(content: string): ConfigType {
  if (content.includes('|type_full_name|') || content.includes('ScopesModule')) {
    return ConfigType.PARSER;
  }
  if (content.includes('|lexeme_type|') || content.includes('base64_encoded_pattern')) {
    return ConfigType.LEXER;
  }
  throw new Error('Неизвестный формат конфигурации');
}

// В функции parseParserConfig, исправьте:
export function parseParserConfig(content: string): ConfigFile {
  const lines = content.split('\n');
  const rows: ParserConfigRow[] = [];
  const comments = new Map<number, string>();

  let lineNumber = 0;
  lines.forEach((line, index) => {
    const trimmed = line.trim();

    // Сохраняем комментарии
    if (trimmed.startsWith('#')) {
      comments.set(index, trimmed);
      return;
    }

    // Пропускаем пустые строки
    if (!trimmed) return;

    lineNumber++;

    // Парсим строку формата: priority|type_full_name|instance_hash|ast_node_type
    const parts = trimmed.split('|');
    if (parts.length !== 4) {
      // Попробуем парсить даже если формат не идеален
      if (parts.length >= 4) {
        // Берем первые 4 части
        const priority = parseFloat(parts[0]);
        const type_full_name = parts[1] || '';
        const instance_hash = parseInt(parts[2], 10) || 0;
        const ast_node_type = parts[3] || '';

        const module = type_full_name.split('.')[0];

        rows.push({
          id: `parser-${index}-${Date.now()}`,
          priority: isNaN(priority) ? 0 : priority,
          type_full_name,
          instance_hash: isNaN(instance_hash) ? 0 : instance_hash,
          ast_node_type,
          module,
          originalLine: trimmed,
          lineNumber,
        });
      }
      return;
    }

    const priority = parseFloat(parts[0]);
    const type_full_name = parts[1];
    const instance_hash = parseInt(parts[2], 10);
    const ast_node_type = parts[3];

    // Извлекаем модуль из type_full_name
    const module = type_full_name.split('.')[0];

    rows.push({
      id: `parser-${index}-${Date.now()}`,
      priority: isNaN(priority) ? 0 : priority,
      type_full_name,
      instance_hash: isNaN(instance_hash) ? 0 : instance_hash,
      ast_node_type,
      module,
      originalLine: trimmed,
      lineNumber,
    });
  });

  return {
    type: ConfigType.PARSER,
    rows,
    fileName: '',
    fileSize: content.length,
    originalContent: content,
    comments,
    lastModified: new Date(),
  };
}

export function parseLexerConfig(content: string): ConfigFile {
  const lines = content.split('\n');
  const rows: LexerConfigRow[] = [];
  const comments = new Map<number, string>();
  
  lines.forEach((line, index) => {
    const trimmed = line.trim();
    
    // Сохраняем комментарии
    if (trimmed.startsWith('#')) {
      comments.set(index, trimmed);
      return;
    }
    
    // Пропускаем пустые строки
    if (!trimmed) return;
    
    // Парсим строку формата: priority|base64_encoded_pattern|lexeme_type|ignore_flag
    const parts = trimmed.split('|');
    if (parts.length !== 4) {
      throw new Error(`Некорректный формат строки ${index + 1}: ${trimmed}`);
    }
    
    const priority = parseFloat(parts[0]);
    const encodedPattern = parts[1];
    const lexeme_type = parts[2];
    const ignore_flag = parts[3].toLowerCase() === 'true';
    
    // Декодируем паттерн из base64
    const decodedPattern = decodeBase64Pattern(encodedPattern);
    
    rows.push({
      id: `lexer-${index}-${Date.now()}`,
      priority,
      encodedPattern,
      decodedPattern,
      lexeme_type,
      ignore_flag,
      originalLine: trimmed,
      lineNumber: index + 1,
    });
  });
  
  return {
    type: ConfigType.LEXER,
    rows,
    fileName: '',
    fileSize: content.length,
    originalContent: content,
    comments,
    lastModified: new Date(),
  };
}

export function formatToOriginal(config: ConfigFile): string {
  const lines: string[] = [];
  const maxLine = Math.max(...config.rows.map(r => r.lineNumber), ...Array.from(config.comments.keys()));
  
  for (let i = 0; i <= maxLine; i++) {
    if (config.comments.has(i)) {
      lines.push(config.comments.get(i)!);
      continue;
    }
    
    const row = config.rows.find(r => r.lineNumber === i);
    if (!row) {
      lines.push('');
      continue;
    }
    
    if (config.type === ConfigType.PARSER) {
      const parserRow = row as ParserConfigRow;
      lines.push(`${parserRow.priority.toFixed(2)}|${parserRow.type_full_name}|${parserRow.instance_hash}|${parserRow.ast_node_type}`);
    } else {
      const lexerRow = row as LexerConfigRow;
      lines.push(`${lexerRow.priority.toFixed(2)}|${encodeBase64Pattern(lexerRow.decodedPattern)}|${lexerRow.lexeme_type}|${lexerRow.ignore_flag ? 'True' : 'False'}`);
    }
  }
  
  return lines.join('\n');
}
