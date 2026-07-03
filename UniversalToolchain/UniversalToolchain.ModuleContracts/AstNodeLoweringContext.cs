namespace UniversalToolchain.ModuleContracts;

public sealed record AstNodeLoweringContext(
    IAstToBytecodeTranslator Translator,
    Bytecode Bytecode);
