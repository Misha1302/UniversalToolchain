import os
import argparse

def read_files_to_single_txt(folder_path, extension, output_file):
    """
    Рекурсивно читает все файлы с указанным расширением и сохраняет их в один файл.
    
    :param folder_path: Путь к папке для обработки
    :param extension: Расширение файлов (например, '.cs')
    :param output_file: Имя выходного файла
    """
    with open(output_file, 'w', encoding='utf-8') as outfile:
        for root, _, files in os.walk(folder_path):
            for file in files:
                if file.endswith(extension):
                    file_path = os.path.join(root, file)
                    
                    # Записываем заголовок файла
                    outfile.write(f"# {file_path}\n")
                    outfile.write("# содержание:\n")
                    
                    # Читаем и записываем содержимое файла
                    try:
                        with open(file_path, 'r', encoding='utf-8') as infile:
                            content = infile.read()
                            outfile.write(content)
                    except UnicodeDecodeError:
                        # Если UTF-8 не работает, пробуем другие кодировки
                        try:
                            with open(file_path, 'r', encoding='cp1252') as infile:
                                content = infile.read()
                                outfile.write(content)
                        except:
                            outfile.write("!!! НЕВОЗМОЖНО ПРОЧИТАТЬ ФАЙЛ !!!\n")
                    except Exception as e:
                        outfile.write(f"!!! ОШИБКА: {str(e)} !!!\n")
                    
                    # Добавляем разделитель
                    outfile.write("\n" + "-" * 35 + "\n\n")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description='Объединение файлов с указанным расширением в один текстовый файл')
    parser.add_argument('folder', help='Путь к исходной папке')
    parser.add_argument('extension', help='Расширение файлов (например: .cs)')
    parser.add_argument('output', help='Имя выходного файла')
    
    args = parser.parse_args()
    
    read_files_to_single_txt(
        folder_path=args.folder,
        extension=args.extension,
        output_file=args.output
    )
    
    print(f"Файлы с расширением {args.extension} из папки {args.folder} объединены в {args.output}")
