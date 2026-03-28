import {ConfigRow, LexerConfigRow, ParserConfigRow, ValidationError} from '@/types/config';
import {RegexValidator} from '@/services/regexValidator';

export function validateParserRow(row: ParserConfigRow): ValidationError[] {
    const errors: ValidationError[] = [];

    // Проверка приоритета
    if (typeof row.priority !== 'number' || isNaN(row.priority)) {
        errors.push({
            rowId: row.id,
            field: 'priority',
            message: 'Приоритет должен быть числом',
            severity: 'error'
        });
    } else if (row.priority < -1000000 || row.priority > 1000000) {
        errors.push({
            rowId: row.id,
            field: 'priority',
            message: 'Приоритет должен быть в диапазоне от -1000000 до 1000000',
            severity: 'error'
        });
    }

    // Проверка полного имени типа
    if (!row.type_full_name || row.type_full_name.trim() === '') {
        errors.push({
            rowId: row.id,
            field: 'type_full_name',
            message: 'Полное имя типа не может быть пустым',
            severity: 'error'
        });
    } else if (!row.type_full_name.includes('.')) {
        errors.push({
            rowId: row.id,
            field: 'type_full_name',
            message: 'Полное имя типа должно содержать точку (Module.TypeName)',
            severity: 'warning'
        });
    }

    // Проверка хэша экземпляра
    if (typeof row.instance_hash !== 'number' || isNaN(row.instance_hash)) {
        errors.push({
            rowId: row.id,
            field: 'instance_hash',
            message: 'Хэш экземпляра должен быть числом',
            severity: 'error'
        });
    }

    // Проверка типа AST-узла
    if (!row.ast_node_type || row.ast_node_type.trim() === '') {
        errors.push({
            rowId: row.id,
            field: 'ast_node_type',
            message: 'Тип AST-узла не может быть пустым',
            severity: 'error'
        });
    }

    return errors;
}

export function validateLexerRow(row: LexerConfigRow): ValidationError[] {
    const errors: ValidationError[] = [];

    // Проверка приоритета
    if (typeof row.priority !== 'number' || isNaN(row.priority)) {
        errors.push({
            rowId: row.id,
            field: 'priority',
            message: 'Приоритет должен быть числом',
            severity: 'error'
        });
    } else if (row.priority < -1000000 || row.priority > 1000000) {
        errors.push({
            rowId: row.id,
            field: 'priority',
            message: 'Приоритет должен быть в диапазоне от -1000000 до 1000000',
            severity: 'error'
        });
    }

    // Проверка паттерна
    if (!row.decodedPattern || row.decodedPattern.trim() === '') {
        errors.push({
            rowId: row.id,
            field: 'decodedPattern',
            message: 'Паттерн не может быть пустым',
            severity: 'error'
        });
    } else {
        const validation = RegexValidator.validate(row.decodedPattern);
        if (!validation.valid) {
            errors.push({
                rowId: row.id,
                field: 'decodedPattern',
                message: `Некорректный регулярный выражение: ${validation.error}`,
                severity: 'error'
            });
        }
    }

    // Проверка типа лексемы
    if (!row.lexeme_type || row.lexeme_type.trim() === '') {
        errors.push({
            rowId: row.id,
            field: 'lexeme_type',
            message: 'Тип лексемы не может быть пустым',
            severity: 'error'
        });
    }

    // Проверка флага игнорирования
    if (typeof row.ignore_flag !== 'boolean') {
        errors.push({
            rowId: row.id,
            field: 'ignore_flag',
            message: 'Флаг игнорирования должен быть true или false',
            severity: 'error'
        });
    }

    return errors;
}

export function validateConfigRow(row: ConfigRow): ValidationError[] {
    if ('type_full_name' in row) {
        return validateParserRow(row);
    } else {
        return validateLexerRow(row);
    }
}

