python3 script_to_one_file.py --pattern '\.cs(?![a-zA-Z0-9])' --root . --exclude-pattern ".*?Tests.*?|.*?bin.*?|.*?obj.*?" --output text.txt
