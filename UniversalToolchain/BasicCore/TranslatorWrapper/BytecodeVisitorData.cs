using BasicCore.ParserWrapper;

namespace BasicCore.TranslatorWrapper;

public record BytecodeVisitorData(IAstToBytecodeTranslator AstToBytecodeTranslator, Bytecode Bytecode, AstNode Node);