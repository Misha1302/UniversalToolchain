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

def process_files(root_dir, extension, exclude_dirs, output_file, pattern=None, exclude_pattern=None):
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
                
                # Записываем в выходной файл
                out_f.write(f"# {file_path}\n")
                out_f.write("# содержание:\n")
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
  
Примечание:
  • Используйте --ext для фильтрации по расширению (старый способ)
  • Используйте --pattern для фильтрации по регулярному выражению для имени файла
  • Используйте --exclude для исключения директорий
  • Используйте --exclude-pattern для исключения по регулярному выражению для пути
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
    
    args = parser.parse_args()
    
    # Проверка, что задан хотя бы один критерий фильтрации файлов
    if not args.ext and not args.pattern:
        parser.error("Необходимо указать либо --ext, либо --pattern")
    
    process_files(args.root, args.ext, args.exclude, args.output, args.pattern, args.exclude_pattern)
    print(f"Обработка завершена. Результат сохранен в {args.output}")
