import {useCallback} from 'react';
import {ConfigRow, ValidationError} from '@/types/config';
import {validateLexerRow, validateParserRow} from '@/services/regexValidator';

export function useValidation() {
    const validateRow = useCallback((row: ConfigRow): ValidationError[] => {
        const errors: ValidationError[] = [];

        // Проверка приоритета для всех типов
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

        // Специфичные проверки
        if ('type_full_name' in row) {
            // Parser row
            const parserErrors = validateParserRow(row);
            parserErrors.forEach(message => {
                errors.push({
                    rowId: row.id,
                    field: 'type_full_name',
                    message,
                    severity: 'error'
                });
            });
        } else if ('decodedPattern' in row) {
            // Lexer row
            const lexerErrors = validateLexerRow(row);
            lexerErrors.forEach(message => {
                errors.push({
                    rowId: row.id,
                    field: 'decodedPattern',
                    message,
                    severity: 'error'
                });
            });
        }

        return errors;
    }, []);

    const validateAllRows = useCallback((rows: ConfigRow[]): ValidationError[] => {
        const errors: ValidationError[] = [];

        rows.forEach(row => {
            const rowErrors = validateRow(row);
            errors.push(...rowErrors);
        });

        // Проверка дубликатов приоритетов
        const priorityMap = new Map<number, string[]>();
        rows.forEach(row => {
            if (!priorityMap.has(row.priority)) {
                priorityMap.set(row.priority, []);
            }
            priorityMap.get(row.priority)!.push(row.id);
        });

        priorityMap.forEach((ids, priority) => {
            if (ids.length > 1) {
                errors.push({
                    rowId: ids[0],
                    field: 'priority',
                    message: `Дублирующийся приоритет ${priority} в ${ids.length} строках`,
                    severity: 'warning'
                });
            }
        });

        return errors;
    }, [validateRow]);

    const getErrorSummary = useCallback((errors: ValidationError[]) => {
        const summary = {
            errors: errors.filter(e => e.severity === 'error').length,
            warnings: errors.filter(e => e.severity === 'warning').length,
            hasErrors: false,
            hasWarnings: false
        };

        summary.hasErrors = summary.errors > 0;
        summary.hasWarnings = summary.warnings > 0;

        return summary;
    }, []);

    return {
        validateRow,
        validateAllRows,
        getErrorSummary
    };
}
