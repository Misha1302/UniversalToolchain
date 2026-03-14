# UniversalToolchain & Wist Language

Readme for January 4 2026 

---

UniversalToolchain is a modular, extensible compiler and interpreter framework for the **Wist** programming language, built on .NET 10. It provides a complete toolchain from source code parsing to execution, featuring both a high-performance compiler (generating dynamic methods) and a flexible interpreter.

## Key Features

- **Modular & Extensible Architecture**: Every language feature (lexing, parsing, IR generation, optimization, execution) is implemented as a pluggable module.
- **Dual Execution Modes**:
  - **Compiler**: Translates Wist code to .NET dynamic methods for near-native performance.
  - **Interpreter**: Executes intermediate representation (IR) directly for flexibility and debugging.
- **Extensible Types & Operations**: Core type system and operations are designed for extension via custom modules.
- **Standard Modules**: Includes arithmetic, conditions, variables, scopes, functions, and native C# interop (not full).
- **Optimization Pipeline**: Supports IR-level optimizations (e.g., local variable optimization, native values optimization).
- **REPL & CLI Tools**: Interactive REPL and command-line runner for rapid development and testing.
- **Comprehensive Diagnostics**: Configurable logging, AST/bytecode dumps, and error reporting.

## Getting Started

### Installation from scratch (.NET 10, official installers)

Choose your OS and install **.NET 10 SDK** using official Microsoft instructions.

#### Windows

~~~powershell
# Install official .NET 10 SDK via winget
winget install Microsoft.DotNet.SDK.10

# Verify
dotnet --version
~~~

#### macOS

Install the official .NET 10 SDK package from Microsoft:

- https://dotnet.microsoft.com/download/dotnet/10.0

Then verify in terminal:

~~~bash
dotnet --version
~~~

#### Linux (Ubuntu 24.04)

~~~bash
# Add official Microsoft package feed
wget https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# Install .NET 10 SDK
sudo apt-get update
sudo apt-get install -y dotnet-sdk-10.0

# Verify
dotnet --version
~~~

#### Linux (Fedora)

~~~bash
# Add official Microsoft package feed
sudo rpm -Uvh https://packages.microsoft.com/config/fedora/$(rpm -E %fedora)/packages-microsoft-prod.rpm

# Install .NET 10 SDK
sudo dnf install -y dotnet-sdk-10.0

# Verify
dotnet --version
~~~

#### Build and run tests

~~~bash
git clone https://github.com/Misha1302/Wist2
cd Wist2
dotnet restore UniversalToolchain/Wist.sln
DOTNET_ROLL_FORWARD=Major dotnet test UniversalToolchain/Tests/Tests.csproj --configuration Release
~~~

### Repository Hygiene

Generated text snapshots (for example files in `UniversalToolchain/project_as_file/*.txt`, `UniversalToolchain/Wistc/code.txt`, and `ConfigurationEditor/project_code.txt`) are local helper artifacts and are intentionally ignored by git.

### Running Wist Code

Use the `wistc` command-line tool (after building):

~~~bash
# Run a Wist file
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run --file program.wist

# Run a one-liner
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- run "let x = 5 + 3; x * 2"

# Start REPL
dotnet run --project UniversalToolchain/Wistc/Wistc.csproj -- repl
~~~

### Example Wist Program

~~~js
// Variables and arithmetic
let a = 10
let b = 20
let sum = a + b

// Conditions
if sum > 25
    sum = sum * 2
else 
    sum = sum / 2

// Function call (C# interop)
System.Console.WriteLine(sum)

// Native arithmetic with type suffixes
let precise = 3.14f * 2.0f
~~~

## Architecture Overview

UniversalToolchain is built around a pipeline of modular stages:

1. **Lexing & Parsing**: Tokenization and AST construction via pluggable lexer/parser modules.
2. **AST to Bytecode**: Translates AST into a platform-independent bytecode.
3. **Bytecode to IR**: Converts bytecode into an intermediate representation (IR) for optimization.
4. **Optimization**: Applies IR-level optimizations via `IIRProcessingModule` implementations.
5. **Compilation/Interpretation**:
   - **Compiler**: Lowers IR to .NET dynamic methods using `GrEmit`.
   - **Interpreter**: Directly executes IR instructions.
6. **Execution**: Runs the compiled method or interpreted IR, returning the result.

Each stage is configurable via dependency injection and module registration.

## Extending the Language

New language features are added by implementing `IFrontendCoreModule` (lexer, parser, AST translator) and/or `IIRProcessingModule` (IR optimizations). Modules are auto-discovered via `AutoRegisterServiceAttribute` or manually registered.

Example module skeleton:

~~~csharp
[AutoRegisterService]
public class MyFeatureModule : IFrontendCoreModule
{
    public void InitLexer(ILexer lexer) { /* Add lexeme patterns */ }
    public void InitParser(IParser parser) { /* Add AST node creators */ }
    public void InitAstTranslator(IAstToBytecodeTranslator translator) { /* Add AST visitors */ }
}
~~~

## Performance

- **Compiler mode** leverages .NET's JIT for high-performance execution, suitable for production workloads.
- **Interpreter mode** prioritizes flexibility and is ideal for debugging, scripting, and educational use.
- Optimizations like local variable caching, and native arithmetic intrinsics are applied where supported.

## License

Licensed under the Apache License 2.0. See [LICENSE](LICENSE) for details.

## Project Rules

**[Project Rules](PROJECT_RULES.md)** - Main rules to write code.
