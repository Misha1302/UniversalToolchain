import os
import re
import argparse

def should_exclude(file_path, exclude_dirs, exclude_pattern=None):
    """Checks whether a path must be excluded by directory or regex pattern."""
    abs_path = os.path.abspath(file_path)
    
    # Check excluded directories.
    for excl_dir in exclude_dirs:
        if os.path.commonpath([abs_path, os.path.abspath(excl_dir)]) == os.path.abspath(excl_dir):
            return True
    
    # Check exclusion regex against full path.
    if exclude_pattern:
        try:
            if re.search(exclude_pattern, abs_path):
                return True
        except re.error as e:
            print(f"Invalid exclusion regex: {e}")
    
    return False

def matches_pattern(filename, pattern=None, extension=None):
    """Checks whether a filename matches a regex pattern or extension."""
    # If a pattern is provided, use it.
    if pattern:
        try:
            return bool(re.search(pattern, filename))
        except re.error as e:
            print(f"Invalid file regex: {e}")
            return False
    
    # Otherwise use extension matching (for backward compatibility).
    if extension:
        # Check file extension.
        if '.' not in filename:
            return False
        file_ext = '.' + filename.rsplit('.', 1)[1]
        return file_ext == extension
    
    return False

def compress_content(content):
    """Removes duplicate whitespace and line breaks from content."""
    # Replace line breaks with spaces.
    content = content.replace('\n', ' ')
    content = content.replace('\r', ' ')
    
    # Collapse repeated whitespace.
    content = re.sub(r'\s+', ' ', content)
    
    # Trim leading/trailing spaces.
    content = content.strip()
    
    return content

def remove_using_directives(content):
    """Removes C# using directives from text."""
    # Split content into lines.
    lines = content.split('\n')
    filtered_lines = []
    
    # Track multiline comment state.
    in_block_comment = False
    
    for line in lines:
        # Handle multiline comments /* ... */.
        if '/*' in line and '*/' not in line:
            in_block_comment = True
            filtered_lines.append(line)
            continue
        
        if in_block_comment:
            if '*/' in line:
                in_block_comment = False
            filtered_lines.append(line)
            continue
        
        # Keep single-line comments unchanged.
        stripped_line = line.strip()
        if stripped_line.startswith('//'):
            filtered_lines.append(line)
            continue
        
        # Check whether the line is a using directive.
        # Pattern: using directive starts with using and ends with ;
        # Exclude using statements (using (var x = ...)).
        if (re.match(r'^\s*(global )?using\s+[^;]+;\s*(\/\/.*)?$', line)
                and not re.match(r'^\s*using\s*\(', line)):
            # This is a using directive, skip it.
            continue
        
        # Not a using directive, keep line.
        filtered_lines.append(line)
    
    return '\n'.join(filtered_lines)

def remove_single_line_comments(content):
    """
    Removes single-line comments (//) while preserving string-literal correctness:
    1. Comments inside string literals are preserved
    2. Raw string literals are treated safely
    3. Escaped quotes are handled safely
    """
    if not content:
        return content
    
    lines = content.split('\n')
    result_lines = []
    
    # State-tracking flags.
    in_string = False          # Inside regular string literal
    in_raw_string = False      # Inside raw string literal
    raw_string_delimiter = 0   # Raw string quote delimiter length
    in_char = False            # Inside char literal
    escape_next = False        # Next character is escaped
    in_block_comment = False   # Inside multiline comment /* ... */
    
    for line in lines:
        i = 0
        result = []
        line_len = len(line)
        
        while i < line_len:
            ch = line[i]
            ch_next = line[i+1] if i+1 < line_len else ''
            ch_prev = line[i-1] if i > 0 else ''
            
            # Process escaping for regular strings.
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
            
            # Check start/end of raw string literal.
            if not in_block_comment and not in_string and not in_char:
                # Check raw string start.
                if ch == '"' and i+2 < line_len and line[i+1:i+3] == '""':
                    # Count opening quotes.
                    j = i
                    while j < line_len and line[j] == '"':
                        j += 1
                    quote_count = j - i
                    
                    if not in_raw_string:
                        # Raw string start.
                        in_raw_string = True
                        raw_string_delimiter = quote_count
                        result.append(line[i:j])
                        i = j
                        continue
                    else:
                        # Check raw string end.
                        if quote_count >= raw_string_delimiter:
                            # Raw string end.
                            in_raw_string = False
                            result.append(line[i:j])
                            i = j
                            continue
            
            # If inside raw string, copy characters as-is.
            if in_raw_string:
                result.append(ch)
                i += 1
                continue
            
            # Check multiline comment start/end.
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
            
            # If inside multiline comment, copy as-is.
            if in_block_comment:
                result.append(ch)
                i += 1
                continue
            
            # Check regular string start/end.
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
            
            # Check single-line comments.
            if not in_string and not in_char and not in_block_comment and not in_raw_string:
                if ch == '/' and ch_next == '/':
                    # Single-line comment found, skip the rest of the line.
                    break
                elif ch == '/' and ch_next == '*':
                    # Multiline comment start (already handled above).
                    pass
            
            # Copy current character.
            result.append(ch)
            i += 1
        
        result_lines.append(''.join(result))
    
    return '\n'.join(result_lines)

def process_files(root_dir, extension, exclude_dirs, output_file, pattern=None, 
                  exclude_pattern=None, compress=False, remove_using=False,
                  remove_comments=False):
    """Processes all matching files and writes merged output."""
    with open(output_file, 'w', encoding='utf-8') as out_f:
        for root, dirs, files in os.walk(root_dir):
            # Skip excluded directories.
            if should_exclude(root, exclude_dirs, exclude_pattern):
                continue
                
            for file in files:
                # Check whether the file matches selection criteria.
                if not matches_pattern(file, pattern, extension):
                    continue
                    
                file_path = os.path.join(root, file)
                
                # Skip files in excluded directories or matching exclude_pattern.
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
                        print(f"Error reading file {file_path}: {str(e)}")
                        continue
                except Exception as e:
                    print(f"Error reading file {file_path}: {str(e)}")
                    continue
                
                # Remove using directives when enabled.
                if remove_using:
                    content = remove_using_directives(content)
                
                # Remove single-line comments when enabled.
                if remove_comments:
                    content = remove_single_line_comments(content)
                
                # Compress content when enabled.
                if compress:
                    content = compress_content(content)
                
                # Write to output file.
                out_f.write(f"# {file_path}\n")
                out_f.write(content)
                out_f.write("\n" + "-" * 40 + "\n\n")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(
        description='Combine source files into a single document',
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Usage examples:
  By extension:      python script.py --ext .py --exclude tests --output result.txt
  By filename regex:
                      python script.py --pattern '.*\\.(py|js)$' --exclude-pattern '.*/test.*' --output result.txt
  By exact filename: python script.py --pattern '^config\\.py$' --output result.txt
  With compression:         python script.py --ext .py --compress --output compressed_result.txt
  Without using directives: python script.py --ext .cs --remove-using --output result.txt
  Without comments:   python script.py --ext .cs --remove-comments --output no_comments.txt
  
Notes:
  • Use --ext for extension filtering (legacy mode)
  • Use --pattern for filename regex filtering
  • Use --exclude to skip directories
  • Use --exclude-pattern to skip paths by regex
  • Use --compress to normalize whitespace
  • Use --remove-using to strip using directives from C# files
  • Use --remove-comments to remove // comments safely
        """
    )
    
    # Mutually exclusive file-filter parameters.
    file_filter_group = parser.add_mutually_exclusive_group()
    file_filter_group.add_argument('--ext', help='File extension (for example: .py, .js)')
    file_filter_group.add_argument('--pattern', help='Filename regex (for example: .*\\.py$)')
    
    parser.add_argument('--root', default='.', help='Root directory for search (default: current directory)')
    parser.add_argument('--exclude', nargs='*', default=[], help='Directories to exclude (for example: tests node_modules)')
    parser.add_argument('--exclude-pattern', help='Path exclusion regex (for example: .*/test.*)')
    parser.add_argument('--output', default='combined_files.txt', help='Output file (default: combined_files.txt)')
    parser.add_argument('--compress', action='store_true', help='Compress file content (normalize whitespace)')
    parser.add_argument('--remove-using', action='store_true', help='Remove using directives from all C# files')
    parser.add_argument('--remove-comments', action='store_true', help='Remove // comments with string-literal awareness')
    
    args = parser.parse_args()
    
    # Require at least one file filter criterion.
    if not args.ext and not args.pattern:
        parser.error("You must specify either --ext or --pattern")
    
    process_files(args.root, args.ext, args.exclude, args.output, args.pattern, 
                  args.exclude_pattern, args.compress, args.remove_using,
                  args.remove_comments)
    print(f"Processing completed. Result saved to {args.output}")
