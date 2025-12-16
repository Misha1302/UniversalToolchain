export class RegexValidator {
  static validate(pattern: string): { valid: boolean; error?: string } {
    if (!pattern.trim()) {
      return { valid: false, error: 'Паттерн не может быть пустым' };
    }
    
    try {
      new RegExp(pattern);
      return { valid: true };
    } catch (error) {
      if (error instanceof SyntaxError) {
        return { 
          valid: false, 
          error: error.message.replace(/^.*?: /, '') 
        };
      }
      return { valid: false, error: 'Неизвестная ошибка' };
    }
  }
  
  static testPattern(pattern: string, testString: string): RegExpMatchArray | null {
    try {
      const regex = new RegExp(pattern);
      return testString.match(regex);
    } catch {
      return null;
    }
  }
  
  static getCaptureGroups(pattern: string): string[] {
    try {
      const regex = new RegExp(pattern);
      const source = regex.source;
      const groups: string[] = [];
      
      // Простая проверка на группы захвата (неполная, но для базового использования)
      const groupRegex = /\((?!\?[:=!<])/g;
      let match;
      while ((match = groupRegex.exec(source)) !== null) {
        groups.push(`Группа ${groups.length + 1}`);
      }
      
      return groups;
    } catch {
      return [];
    }
  }
  
  static isPotentiallyDangerous(pattern: string): boolean {
    // Проверка на потенциально опасные паттерны (редукция катастрофы)
    const dangerousPatterns = [
      /\(.*\)*\+/,  // Вложенные повторы
      /\*\{.*,\}.*\*/,  // Большие диапазоны повторов
      /\\d\+\+/,  // Жадные повторы с цифрами
    ];
    
    return dangerousPatterns.some(dangerous => dangerous.test(pattern));
  }
  
  static simplifyForPreview(pattern: string, maxLength: number = 50): string {
    if (pattern.length <= maxLength) return pattern;
    
    const half = Math.floor(maxLength / 2) - 2;
    return pattern.substring(0, half) + '...' + pattern.substring(pattern.length - half);
  }
}

export function validateLexerRow(row: any) {
  const errors: string[] = [];
  
  // Проверка приоритета
  if (typeof row.priority !== 'number' || isNaN(row.priority)) {
    errors.push('Приоритет должен быть числом');
  } else if (row.priority < -1000000 || row.priority > 1000000) {
    errors.push('Приоритет должен быть в диапазоне от -1000000 до 1000000');
  }
  
  // Проверка паттерна
  if (!row.decodedPattern || row.decodedPattern.trim() === '') {
    errors.push('Паттерн не может быть пустым');
  } else {
    const validation = RegexValidator.validate(row.decodedPattern);
    if (!validation.valid) {
      errors.push(`Некорректный регулярный выражение: ${validation.error}`);
    }
  }
  
  // Проверка типа лексемы
  if (!row.lexeme_type || row.lexeme_type.trim() === '') {
    errors.push('Тип лексемы не может быть пустым');
  }
  
  return errors;
}

export function validateParserRow(row: any) {
  const errors: string[] = [];
  
  // Проверка приоритета
  if (typeof row.priority !== 'number' || isNaN(row.priority)) {
    errors.push('Приоритет должен быть числом');
  }
  
  // Проверка полного имени типа
  if (!row.type_full_name || row.type_full_name.trim() === '') {
    errors.push('Полное имя типа не может быть пустым');
  } else if (!row.type_full_name.includes('.')) {
    errors.push('Полное имя типа должно содержать точку (Module.TypeName)');
  }
  
  // Проверка хэша экземпляра
  if (typeof row.instance_hash !== 'number' || isNaN(row.instance_hash)) {
    errors.push('Хэш экземпляра должен быть числом');
  }
  
  // Проверка типа AST-узла
  if (!row.ast_node_type || row.ast_node_type.trim() === '') {
    errors.push('Тип AST-узла не может быть пустым');
  }
  
  return errors;
}
