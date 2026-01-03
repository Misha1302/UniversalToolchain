import os
import re
import argparse

def should_exclude(file_path, exclude_dirs, exclude_pattern=None):
    """Проверяет, находится ли файл в исключенной директории или соответствует ли исключающему шаблону."""
    abs_path = os.path.abspath(file_path)
    
    # Проверка по списку исключенных директорий
    for excl_dir in exclude_dirs:
        if os.path.commonpath([abs_path, os.path.abspath(excl_dir)]) == os.path.abspath(excl_dir):
            return True
    
    # Проверка по регулярному выражению для пути
    if exclude_pattern:
        try:
            if re.search(exclude_pattern, abs_path):
                return True
        except re.error as e:
            print(f"Ошибка в регулярном выражении для исключения: {e}")
    
    return False

def matches_pattern(filename, pattern=None, extension=None):
    """Проверяет, соответствует ли имя файла заданному шаблону или расширению."""
    # Если задан паттерн, используем его
    if pattern:
        try:
            return bool(re.search(pattern, filename))
        except re.error as e:
            print(f"Ошибка в регулярном выражении для файлов: {e}")
            return False
    
    # Иначе используем расширение (для обратной совместимости)
    if extension:
        # Проверяем расширение файла
        if '.' not in filename:
            return False
        file_ext = '.' + filename.rsplit('.', 1)[1]
        return file_ext == extension
    
    return False

def compress_content(content):
    """Удаляет двойные пробелы и переносы строк из содержимого."""
    # Заменяем все переносы строк на пробелы
    content = content.replace('\n', ' ')
    content = content.replace('\r', ' ')
    
    # Заменяем множественные пробелы на один пробел
    content = re.sub(r'\s+', ' ', content)
    
    # Удаляем пробелы в начале и конце
    content = content.strip()
    
    return content

def remove_using_directives(content):
    """Удаляет директивы using из C# кода."""
    # Разбиваем содержимое на строки
    lines = content.split('\n')
    filtered_lines = []
    
    # Флаг для отслеживания многострочных комментариев
    in_block_comment = False
    
    for line in lines:
        # Обработка многострочных комментариев /* ... */
        if '/*' in line and '*/' not in line:
            in_block_comment = True
            filtered_lines.append(line)
            continue
        
        if in_block_comment:
            if '*/' in line:
                in_block_comment = False
            filtered_lines.append(line)
            continue
        
        # Игнорируем однострочные комментарии
        stripped_line = line.strip()
        if stripped_line.startswith('//'):
            filtered_lines.append(line)
            continue
        
        # Проверяем, является ли строка using директивой
        # Шаблон для using директив: начинается с 'using', заканчивается ';'
        # Но не является using statement (using (var x = ...))
        if (re.match(r'^\s*(global )?using\s+[^;]+;\s*(\/\/.*)?$', line)
                and not re.match(r'^\s*using\s*\(', line)):
            # Это using директива, пропускаем ее
            continue
        
        # Если это не using директива, добавляем строку
        filtered_lines.append(line)
    
    return '\n'.join(filtered_lines)

def remove_single_line_comments(content):
    """
    Удаляет однострочные комментарии (//), но учитывает специфические случаи:
    1. Комментарии внутри строковых литералов не удаляются
    2. Сырые строковые литералы (raw string literals) не обрабатываются для комментариев внутри них
    3. Безопасное удаление комментариев с учетом экранирования кавычек
    """
    if not content:
        return content
    
    lines = content.split('\n')
    result_lines = []
    
    # Флаги для отслеживания состояния
    in_string = False          # Находимся внутри обычной строки ""
    in_raw_string = False      # Находимся внутри сырой строки (raw string literal)
    raw_string_delimiter = 0   # Количество кавычек для сырой строки
    in_char = False            # Находимся внутри символьного литерала ''
    escape_next = False        # Следующий символ экранирован
    in_block_comment = False   # Находимся внутри многострочного комментария /* ... */
    
    for line in lines:
        i = 0
        result = []
        line_len = len(line)
        
        while i < line_len:
            ch = line[i]
            ch_next = line[i+1] if i+1 < line_len else ''
            ch_prev = line[i-1] if i > 0 else ''
            
            # Обработка экранирования для обычных строк
            if not in_raw_string and not in_block_comment and not in_char:
                if ch == '\\' and in_string:
                    escape_next = not escape_next
                    result.append(ch)
                    i += 1
                    continue
                elif escape_next:
                    escape_next = False
                    result.append(ch)
                    i += 1
                    continue
            
            # Проверка начала/конца сырой строки (raw string literal)
            if not in_block_comment and not in_string and not in_char:
                # Проверяем начало сырой строки
                if ch == '"' and i+2 < line_len and line[i+1:i+3] == '""':
                    # Считаем количество открывающих кавычек
                    j = i
                    while j < line_len and line[j] == '"':
                        j += 1
                    quote_count = j - i
                    
                    if not in_raw_string:
                        # Начало сырой строки
                        in_raw_string = True
                        raw_string_delimiter = quote_count
                        result.append(line[i:j])
                        i = j
                        continue
                    else:
                        # Проверяем конец сырой строки
                        if quote_count >= raw_string_delimiter:
                            # Конец сырой строки
                            in_raw_string = False
                            result.append(line[i:j])
                            i = j
                            continue
            
            # Если мы внутри сырой строки, просто копируем символы
            if in_raw_string:
                result.append(ch)
                i += 1
                continue
            
            # Проверка начала/конца многострочного комментария
            if not in_string and not in_char and not in_raw_string:
                if ch == '/' and ch_next == '*':
                    in_block_comment = True
                    result.append(ch)
                    result.append(ch_next)
                    i += 2
                    continue
                elif ch == '*' and ch_next == '/' and in_block_comment:
                    in_block_comment = False
                    result.append(ch)
                    result.append(ch_next)
                    i += 2
                    continue
            
            # Если внутри многострочного комментария, просто копируем
            if in_block_comment:
                result.append(ch)
                i += 1
                continue
            
            # Проверка начала/конца строкового литерала
            if not in_block_comment and not in_raw_string:
                if ch == '"' and not in_char and not (in_string and escape_next):
                    in_string = not in_string
                    result.append(ch)
                    i += 1
                    continue
                elif ch == "'" and not in_string and not (in_char and escape_next):
                    in_char = not in_char
                    result.append(ch)
                    i += 1
                    continue
            
            # Проверка на однострочный комментарий
            if not in_string and not in_char and not in_block_comment and not in_raw_string:
                if ch == '/' and ch_next == '/':
                    # Найден однострочный комментарий, пропускаем остаток строки
                    break
                elif ch == '/' and ch_next == '*':
                    # Начало многострочного комментария (уже обработано выше)
                    pass
            
            # Копируем текущий символ
            result.append(ch)
            i += 1
        
        result_lines.append(''.join(result))
    
    return '\n'.join(result_lines)

def process_files(root_dir, extension, exclude_dirs, output_file, pattern=None, 
                  exclude_pattern=None, compress=False, remove_using=False,
                  remove_comments=False):
    """Обрабатывает все файлы по заданному критерию и записывает результат."""
    with open(output_file, 'w', encoding='utf-8') as out_f:
        for root, dirs, files in os.walk(root_dir):
            # Пропускаем исключенные директории
            if should_exclude(root, exclude_dirs, exclude_pattern):
                continue
                
            for file in files:
                # Проверяем, соответствует ли файл критерию отбора
                if not matches_pattern(file, pattern, extension):
                    continue
                    
                file_path = os.path.join(root, file)
                
                # Пропускаем файлы в исключенных директориях или соответствующие exclude_pattern
                if should_exclude(file_path, exclude_dirs, exclude_pattern):
                    continue
                
                try:
                    with open(file_path, 'r', encoding='utf-8') as in_f:
                        content = in_f.read()
                except UnicodeDecodeError:
                    try:
                        with open(file_path, 'r', encoding='latin-1') as in_f:
                            content = in_f.read()
                    except Exception as e:
                        print(f"Ошибка чтения файла {file_path}: {str(e)}")
                        continue
                except Exception as e:
                    print(f"Ошибка чтения файла {file_path}: {str(e)}")
                    continue
                
                # Удаляем using директивы из всех файлов, если флаг установлен
                if remove_using:
                    content = remove_using_directives(content)
                
                # Удаляем однострочные комментарии, если флаг установлен
                if remove_comments:
                    content = remove_single_line_comments(content)
                
                # Сжимаем содержимое, если включен флаг compress
                if compress:
                    content = compress_content(content)
                
                # Записываем в выходной файл
                out_f.write(f"# {file_path}\n")
                out_f.write(content)
                out_f.write("\n" + "-" * 40 + "\n\n")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(
        description='Объединение файлов с кодом в один документ',
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Примеры использования:
  По расширению:      python script.py --ext .py --exclude tests --output result.txt
  По регулярному выражению для имени файла:
                      python script.py --pattern '.*\\.(py|js)$' --exclude-pattern '.*/test.*' --output result.txt
  По конкретному имени: python script.py --pattern '^config\\.py$' --output result.txt
  Со сжатием:         python script.py --ext .py --compress --output compressed_result.txt
  Без using директив: python script.py --ext .cs --remove-using --output result.txt
  Без комментариев:   python script.py --ext .cs --remove-comments --output no_comments.txt
  
Примечание:
  • Используйте --ext для фильтрации по расширению (старый способ)
  • Используйте --pattern для фильтрации по регулярному выражению для имени файла
  • Используйте --exclude для исключения директорий
  • Используйте --exclude-pattern для исключения по регулярному выражению для пути
  • Используйте --compress для сжатия содержимого (удаление двойных пробелов и переносов строк)
  • Используйте --remove-using для удаления using директив из всех C# файлов
  • Используйте --remove-comments для удаления однострочных комментариев (//) с учетом строковых литералов
        """
    )
    
    # Группа параметров для фильтрации файлов (взаимоисключающие)
    file_filter_group = parser.add_mutually_exclusive_group()
    file_filter_group.add_argument('--ext', help='Расширение файлов (например: .py, .js)')
    file_filter_group.add_argument('--pattern', help='Регулярное выражение для имени файла (например: .*\\.py$)')
    
    parser.add_argument('--root', default='.', help='Корневая директория для поиска (по умолчанию: текущая директория)')
    parser.add_argument('--exclude', nargs='*', default=[], help='Директории для исключения (например: tests node_modules)')
    parser.add_argument('--exclude-pattern', help='Регулярное выражение для исключения путей (например: .*/test.*)')
    parser.add_argument('--output', default='combined_files.txt', help='Выходной файл (по умолчанию: combined_files.txt)')
    parser.add_argument('--compress', action='store_true', help='Сжимать содержимое файлов (удалять двойные пробелы и переносы строк)')
    parser.add_argument('--remove-using', action='store_true', help='Удалять using директивы из всех C# файлов')
    parser.add_argument('--remove-comments', action='store_true', help='Удалять однострочные комментарии (//) с учетом строковых литералов')
    
    args = parser.parse_args()
    
    # Проверка, что задан хотя бы один критерий фильтрации файлов
    if not args.ext and not args.pattern:
        parser.error("Необходимо указать либо --ext, либо --pattern")
    
    process_files(args.root, args.ext, args.exclude, args.output, args.pattern, 
                  args.exclude_pattern, args.compress, args.remove_using,
                  args.remove_comments)
    print(f"Обработка завершена. Результат сохранен в {args.output}")
