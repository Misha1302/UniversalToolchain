export enum ConfigType {
  PARSER = 'parser',
  LEXER = 'lexer'
}

export interface BaseConfigRow {
  id: string;
  priority: number;
  originalLine?: string;
  lineNumber: number;
  errors?: ValidationError[];
}

export interface ParserConfigRow extends BaseConfigRow {
  type_full_name: string;
  instance_hash: number;
  ast_node_type: string;
  module?: string;
}

export interface LexerConfigRow extends BaseConfigRow {
  encodedPattern: string;
  decodedPattern: string;
  lexeme_type: string;
  ignore_flag: boolean;
  isValidRegex?: boolean;
  regexError?: string;
}

export type ConfigRow = ParserConfigRow | LexerConfigRow;

export interface ConfigFile {
  type: ConfigType;
  rows: ConfigRow[];
  fileName: string;
  fileSize: number;
  originalContent: string;
  comments: Map<number, string>;
  lastModified: Date;
}

export interface ValidationError {
  rowId: string;
  field: string;
  message: string;
  severity: 'error' | 'warning';
}