#!/bin/bash

# Parse CLI arguments
COMPRESS_FLAG=""
REMOVE_USING_FLAG=""
REMOVE_COMMENTS_FLAG=""

while [[ $# -gt 0 ]]; do
    case $1 in
        --compress)
            COMPRESS_FLAG="--compress"
            shift
            ;;
        --remove-using)
            REMOVE_USING_FLAG="--remove-using"
            shift
            ;;
        --remove-comments)
            REMOVE_COMMENTS_FLAG="--remove-comments"
            shift
            ;;
        *)
            echo "Unknown argument: $1"
            echo "Usage: $0 [--compress] [--remove-using] [--output OUTPUT_FILE]"
            exit 1
            ;;
    esac
done

if [[ -n "$COMPRESS_FLAG" ]]; then
    echo "Compression mode enabled"
fi

if [[ -n "$REMOVE_USING_FLAG" ]]; then
    echo "Remove-using mode enabled"
fi

if [[ -n "$REMOVE_COMMENTS_FLAG" ]]; then
    echo "Remove-comments mode enabled"
fi

python3 script_to_one_file.py --pattern '\.cs(?![a-zA-Z0-9])|\.md' --root ./.. --exclude-pattern '.*?(Tests|Benchmark|Extensions|Wistc|Comments|Scopes|Whitespaces).*?|.*?(?<![a-zA-Z])(bin|obj)(?![a-zA-Z]).*?' --output "partial_main_code.txt" $COMPRESS_FLAG $REMOVE_USING_FLAG $REMOVE_COMMENTS_FLAG

python3 script_to_one_file.py --pattern '\.cs(?![a-zA-Z0-9])|\.md' --root ./.. --exclude-pattern '.*?(Tests|Benchmarks).*?|.*?(?<![a-zA-Z])(bin|obj)(?![a-zA-Z]).*?' --output "main_code.txt" $COMPRESS_FLAG $REMOVE_USING_FLAG $REMOVE_COMMENTS_FLAG

python3 script_to_one_file.py --pattern '\.cs(?![a-zA-Z0-9])|\.md' --root ./.. --exclude-pattern '^(?!.*Benchmarks).+$|.*?(?<![a-zA-Z])(bin|obj)(?![a-zA-Z]).*?' --output "benchmarks.txt" $COMPRESS_FLAG $REMOVE_USING_FLAG $REMOVE_COMMENTS_FLAG

python3 script_to_one_file.py --pattern '\.cs(?![a-zA-Z0-9])|\.md' --root ./.. --exclude-pattern '^(?!.*Tests).+$|.*?(?<![a-zA-Z])(bin|obj)(?![a-zA-Z]).*?' --output "tests.txt" $COMPRESS_FLAG $REMOVE_USING_FLAG

python3 script_to_one_file.py --pattern '\.cs(?![a-zA-Z0-9])|\.md' --root ./.. --exclude-pattern '.*?(?<![a-zA-Z])(bin|obj)(?![a-zA-Z]).*?' --output "all.txt" $COMPRESS_FLAG $REMOVE_USING_FLAG $REMOVE_COMMENTS_FLAG


