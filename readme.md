# UniversalToolchain & Wist Language

Documentation for 20 december 2025

## 1. Project Overview

**UniversalToolchain** is a modular, extensible framework for building domain-specific languages (DSLs) and scripting
languages in C#. At its core is **Wist** - a dynamically-typed language designed for embedding and extensibility, with
its compiler **Wistc** providing command-line access.

The framework implements a complete compilation pipeline: lexical analysis, syntax parsing, bytecode translation, IL
compilation, and execution. Its plugin architecture allows developers to add language features incrementally, making it
suitable for creating custom DSLs for various domains.

### Key Capabilities:

- Modular language feature system
- Full compilation pipeline implementation
- Runtime IL generation and execution
- Extensible AST and bytecode visitors
- C# interoperability
- Detailed debugging and logging tools

## 2. Core Architecture

### Pipeline Architecture

The compilation process follows a strict pipeline:

```
Source Code
-> Lexical Analysis (Lexer)
-> Syntax Parsing (Parser)
-> AST Transformation
-> Bytecode Generation
-> IL Compilation
-> Execution/Interpretation
```

Each stage can be modified or extended by registered modules.

### Component Breakdown

- **BasicCore** - The central orchestrator coordinating the pipeline execution. It manages module registration and data
  flow between stages.
- **ExtensibleEnum System** - A dynamic enumeration system for lexeme and AST node types that allows runtime extension
  without recompilation.
- **Module System** - Each feature (arithmetic, variables, conditions) is implemented as an `ICoreModule` that can hook
  into any pipeline stage.

## 3. Available Modules

The framework includes these core language modules:

### Basic Language Features:

- **IdentifierModule** - Recognizes identifiers with support for namespaces and generics
- **NumbersModule** - Numeric literals with RealNumberImpl type
- **WhitespacesModule** - Whitespace handling and newline recognition
- **SemicolonAsNewLineModule** - Treats semicolons as statement terminators

### Arithmetic & Logic:

- **ArithmeticModule** - Basic arithmetic operations (+, -, *, /)
- **ConditionsModule** - Conditional statements (if/elif/else)
- **ComparisonOperations** - Relational operators (==, !=, >, <, >=, <=)
- **BooleanOperations** - Boolean logic (and, or, not, true, false)

### Data & Control Flow:

- **VariablesModule** - Variable declaration with let syntax and type inference
- **EqualityModule** - Assignment operations
- **LabelsModule** - Labels and goto statements
- **ScopesModule** - Parentheses and scope boundaries

### Interoperability:

- **CSharpInteropModule** - Direct calling of static C# methods

### Development Tools:

- **ExecutorLoggerModule** - Comprehensive logging of all pipeline stages
- **ParserConfigurationModule** - Dump/load parser configuration for debugging
- **LexerConfigurationModule** - Manage lexer pattern priorities

## 4. Getting Started

### Installation

**As a Library:**

```bash
# Clone the repository
git clone <repository-url>
cd UniversalToolchain

# Build the solution
dotnet build Wist.sln
```

**Using the CLI Tool:**

```bash
# Navigate to Wistc directory and build
cd Wistc
dotnet publish -c Release -o ./dist
```

### Basic Usage Examples

**Simple Arithmetic:**

```js
// Basic arithmetic expression
2 + 3 * (4 - 1)
```

**Variables and Assignment:**:

```js
// Variable declaration and usage
let x = 5
let y = x * 2
y = y + 3
y
```

**Conditional Logic:**

You may not write parenthesis if the **condition** and the **body** can be parsed as a **single** expression

```js
// If-else with comparisons
if x > 3 and y < 20
    "small"
elif x == 5
    "five"
else
    "other"
```

## 5. Extending the Language

### Creating a Custom Module

Implement the ICoreModule interface to add new language features:

```c#
public class CustomModule : ICoreModule
{
    public void InitLexer(ILexer lexer)
    {
        // Register new lexeme patterns
        lexer.Configuration.TryAddPattern(
            new LexemePattern(@"custom", 
                ExtensibleEnum<LexemeTag>.CreateOrGet("CustomKeyword"))
        );
    }
    
    public void InitParser(IParser parser)
    {
        // Register AST node creators
        parser.Configuration.NodeCreators.Add(
            10f, new CustomNodeCreator()
        );
    }
    
    public void InitTranslator(IBytecodeTranslator translator)
    {
        // Register bytecode visitors
        translator.Configuration.Visitors.Add(
            new CustomAstVisitor()
        );
    }

    // ...other interface methods...
}
```

### Module Registration

Add your module to the core initialization:

```c#
var core = new BasicCoreImpl(
    () => new BasicLexerImpl(),
    () => new BasicParserImpl(),
    () => new BasicBytecodeTranslatorImpl(),
    () => new BytecodeDynamicMethodsCompilerImpl(),
    () => new BasicInterpreterImpl(),
    [
        new CustomModule(),
        // Other modules...
    ]
);
```

## 6. Advanced Features

### Debugging Tools

* Configuration Editor - A web-based tool for inspecting and modifying lexer/parser configurations.
* Logs Viewer - Visualizes compilation logs including:
    1. Source code with syntax highlighting
    2. Lexeme tokenization results
    3. AST tree visualization
    4. Bytecode instruction listing
    5. Generated CIL code

### Configuration Management

Parser and lexer module execution order can be serialized, edited, and reloaded, enabling precise control over language
feature interactions.
Generic Method Support

### AST Tagging System

AST nodes support custom tags for metadata propagation through the compilation pipeline.

## 7. CLI Tool (Wistc)

The Wistc compiler provides command-line access:

```bash
# Basic compilation and execution
wistc -s source.wist

# With detailed logging
wistc -s source.wist -l logs.txt

# Dump parser configuration for debugging
wistc -s source.wist --parser-configuration-dump

# Load custom parser configuration
wistc -s source.wist --parser-configuration-read --parser-configuration config.txt
```

Options:

* `-s, --source`: Path to source file (required)
* `-l, --logs`: Path for log output
* `--parser-configuration`: Configuration file path
* `--parser-configuration-read`: Load configuration from file
* `--parser-configuration-dump`: Save current configuration to file

## 8. Development & Contribution

### Requirements

* .NET 9.0 SDK or later
* Understanding of compiler construction concepts
* Familiarity with C# and IL generation

### Building from Source

```bash
# Clone the repository
git clone <repository-url>

# Restore dependencies
dotnet restore Wist.sln

# Build all projects
dotnet build Wist.sln -c Release

# Run tests
dotnet test Tests/Tests.csproj
```

### Code Style Guidelines

* Use explicit null checks with NotNull() extension
* Prefer immutability where possible
* Document public APIs with XML comments
* Follow existing naming conventions

### Testing

The test suite includes:

* Unit tests for individual modules
* Integration tests for the full pipeline
* Performance and edge case tests
* Real-world scenario simulations

## 9. Limitations & Future Work

Planned Enhancements:

* Switching from CIL to a new simplified final high-level stack-based bytecode
* Add advanced type system (more basic types, structures, classes, system-F and others)
* Advanced error reporting
* Implement good compiler/jit with optimization
* Add IDE support
* Implement more plugins for better ecosystem
* Implement standard libraries

## 10. License

Licensed under the Apache License, Version 2.0;
you may not use this project except in compliance with the License.

## 11. Rules & Guidelines

If you want to write code using this project, please read our project rules:
- **[Project Rules](PROJECT_RULES.md)** - Main rules to write code