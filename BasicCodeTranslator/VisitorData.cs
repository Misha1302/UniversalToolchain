// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using BasicCore;

namespace BasicCodeTranslator;

public record VisitorData(IBytecodeTranslator BytecodeTranslator, Bytecode Bytecode, AstNode Node);