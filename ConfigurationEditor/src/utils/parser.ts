import { ConfigType, ConfigFile, ParserConfigRow, LexerConfigRow } from '@/types/config';
import { decodeBase64Pattern, encodeBase64Pattern } from './base64';

export function detectConfigType(content: string): ConfigType {
  if (content.includes('|type_full_name|') || content.includes('ScopesModule')) {
    return ConfigType.PARSER;
  }
  if (content.includes('|lexeme_type|') || content.includes('base64_encoded_pattern')) {
    return ConfigType.LEXER;
  }
  throw new Error('Unknown configuration format');
}

export function parseParserConfig(content: string): ConfigFile {
  const lines = content.split('\n');
  const rows: ParserConfigRow[] = [];
  const comments = new Map<number, string>();

  lines.forEach((line, index) => {
    const trimmed = line.trim();

    if (trimmed.startsWith('#')) {
      comments.set(index + 1, trimmed);
      return;
    }

    if (!trimmed) return;

    const parts = trimmed.split('|');
    if (parts.length !== 4) {
      if (parts.length >= 4) {
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
          lineNumber: index + 1,
        });
      }
      return;
    }

    const priority = parseFloat(parts[0]);
    const type_full_name = parts[1];
    const instance_hash = parseInt(parts[2], 10);
    const ast_node_type = parts[3];
    const module = type_full_name.split('.')[0];

    rows.push({
      id: `parser-${index}-${Date.now()}`,
      priority: isNaN(priority) ? 0 : priority,
      type_full_name,
      instance_hash: isNaN(instance_hash) ? 0 : instance_hash,
      ast_node_type,
      module,
      originalLine: trimmed,
      lineNumber: index + 1,
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

    if (trimmed.startsWith('#')) {
      comments.set(index + 1, trimmed);
      return;
    }

    if (!trimmed) return;

    const parts = trimmed.split('|');
    if (parts.length !== 4) {
      throw new Error(`Invalid line format ${index + 1}: ${trimmed}`);
    }

    const priority = parseFloat(parts[0]);
    const encodedPattern = parts[1];
    const lexeme_type = parts[2];
    const ignore_flag = parts[3].toLowerCase() === 'true';
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

function normalizeCommentsMap(comments: ConfigFile['comments']): Map<number, string> {
  if (comments && typeof comments === 'object') {
    if (comments instanceof Map) {
      return comments;
    }

    const map = new Map<number, string>();
    Object.entries(comments).forEach(([key, value]) => {
      map.set(parseInt(key, 10), String(value));
    });

    return map;
  }

  return new Map<number, string>();
}

export function formatToOriginal(config: ConfigFile): string {
  const lines: string[] = [];
  const commentsMap = normalizeCommentsMap(config.comments);
  const rowsByLine = new Map(config.rows.map(row => [row.lineNumber, row]));

  const allLineNumbers = [
    ...config.rows.map(row => row.lineNumber),
    ...Array.from(commentsMap.keys()),
  ];

  if (allLineNumbers.length === 0) {
    return '';
  }

  const maxLineNumber = Math.max(...allLineNumbers);

  for (let lineNum = 1; lineNum <= maxLineNumber; lineNum++) {
    if (commentsMap.has(lineNum)) {
      lines.push(commentsMap.get(lineNum)!);
    }

    const row = rowsByLine.get(lineNum);
    if (row) {
      if (config.type === ConfigType.PARSER) {
        const parserRow = row as ParserConfigRow;
        lines.push(`${parserRow.priority.toFixed(2)}|${parserRow.type_full_name}|${parserRow.instance_hash}|${parserRow.ast_node_type}`);
      } else {
        const lexerRow = row as LexerConfigRow;
        const encodedPattern = lexerRow.encodedPattern || encodeBase64Pattern(lexerRow.decodedPattern);
        lines.push(`${lexerRow.priority.toFixed(2)}|${encodedPattern}|${lexerRow.lexeme_type}|${lexerRow.ignore_flag ? 'True' : 'False'}`);
      }
    }
  }

  return lines.join('\n');
}
