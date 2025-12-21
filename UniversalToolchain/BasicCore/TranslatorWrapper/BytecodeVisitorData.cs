using BasicCore.ParserWrapper;

namespace BasicCore.TranslatorWrapper;

public record BytecodeVisitorData(IBytecodeTranslator BytecodeTranslator, Bytecode Bytecode, AstNode Node);