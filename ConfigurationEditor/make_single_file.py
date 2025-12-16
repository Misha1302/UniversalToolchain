#!/usr/bin/env python3
import os
from pathlib import Path

def quick_collect(root_dir='.', output_file='project_code.txt'):
    """Быстрый сборщик - игнорирует все ненужное"""
    root = Path(root_dir).resolve()
    output = Path(output_file)
    
    # Все что нужно игнорировать
    IGNORE = {
        # Директории
        'node_modules', '.git', '.vscode', '.idea',
        'dist', 'build', '.next', '.nuxt', '.astro',
        'coverage', '__pycache__', '.cache', 'public',
        'static', 'assets', 'images', 'fonts',
        
        # Файлы
        'package-lock.json', 'yarn.lock', 'pnpm-lock.yaml',
        'bun.lockb', 'composer.lock', 'Gemfile.lock',
        'go.sum', 'Cargo.lock', '.DS_Store', 'Thumbs.db',
        '*.log', '.env.local', '.env.production',
        
        # Расширения
        '.png', '.jpg', '.jpeg', '.gif', '.svg',
        '.woff', '.woff2', '.ttf', '.eot', '.otf',
        '.mp4', '.mp3', '.wav', '.zip', '.tar',
        '.gz', '.rar', '.exe', '.dll', '.so',
        '.dylib', '.pdf', '.doc', '.docx',
        '.map', '.snap', '.min.js', '.min.css',
    }
    
    files = []
    for file_path in root.rglob('*'):
        if file_path.is_file():
            skip = False
            
            # Проверяем директории в пути
            for part in file_path.parts:
                if part in IGNORE:
                    skip = True
                    break
            
            # Проверяем файлы
            if file_path.name in IGNORE:
                skip = True
            
            # Проверяем расширения
            if file_path.suffix.lower() in IGNORE:
                skip = True
            
            # Пропускаем скрытые файлы
            if file_path.name.startswith('.'):
                if file_path.name not in ['.env', '.gitignore']:
                    skip = True
            
            # Пропускаем большие файлы
            if file_path.stat().st_size > 2 * 1024 * 1024:
                skip = True
            
            if not skip:
                files.append(file_path)
    
    # Сортируем
    files.sort(key=lambda x: (x.suffix, str(x)))
    
    # Записываем
    with open(output, 'w', encoding='utf-8') as f:
        f.write(f"# Код проекта: {root.name}\n\n")
        
        for file_path in files:
            rel_path = file_path.relative_to(root)
            f.write(f"\n{'='*80}\n")
            f.write(f"ФАЙЛ: {rel_path}\n")
            f.write(f"{'='*80}\n\n")
            
            try:
                content = file_path.read_text(encoding='utf-8')
                f.write(content)
            except:
                try:
                    content = file_path.read_text(encoding='cp1251')
                    f.write(content)
                except:
                    f.write("[БИНАРНЫЙ ФАЙЛ]\n")
            
            f.write("\n")
    
    print(f"✅ Собрано {len(files)} файлов в {output}")

if __name__ == '__main__':
    import sys
    root = sys.argv[1] if len(sys.argv) > 1 else '.'
    output = sys.argv[2] if len(sys.argv) > 2 else 'project_code.txt'
    quick_collect(root, output)
