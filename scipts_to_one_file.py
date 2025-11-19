import os
import argparse

def should_exclude(file_path, exclude_dirs):
    """Проверяет, находится ли файл в исключенной директории."""
    abs_path = os.path.abspath(file_path)
    for excl_dir in exclude_dirs:
        if os.path.commonpath([abs_path, os.path.abspath(excl_dir)]) == os.path.abspath(excl_dir):
            return True
    return False

def process_files(root_dir, extension, exclude_dirs, output_file):
    """Обрабатывает все файлы с заданным расширением и записывает результат."""
    with open(output_file, 'w', encoding='utf-8') as out_f:
        for root, dirs, files in os.walk(root_dir):
            # Пропускаем исключенные директории
            if should_exclude(root, exclude_dirs):
                continue
                
            for file in files:
                if '.' not in file: continue

                if '.' + file.split('.', 1)[1] == extension:
                    file_path = os.path.join(root, file)
                    
                    # Пропускаем файлы в исключенных поддиректориях
                    if should_exclude(file_path, exclude_dirs):
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
    parser = argparse.ArgumentParser(description='Объединение файлов с кодом в один документ')
    parser.add_argument('--root', default='.', help='Корневая директория для поиска (по умолчанию: текущая директория)')
    parser.add_argument('--ext', required=True, help='Расширение файлов (например: .cs)')
    parser.add_argument('--exclude', nargs='*', default=[], help='Директории для исключения (например: Tests)')
    parser.add_argument('--output', default='combined_files.txt', help='Выходной файл (по умолчанию: combined_files.txt)')
    
    args = parser.parse_args()
    
    process_files(args.root, args.ext, args.exclude, args.output)
    print(f"Обработка завершена. Результат сохранен в {args.output}")