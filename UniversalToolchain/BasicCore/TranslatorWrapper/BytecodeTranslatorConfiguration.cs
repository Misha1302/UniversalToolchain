using BasicCore.ParserWrapper;

namespace BasicCore.TranslatorWrapper;

public record BytecodeTranslatorConfiguration(List<IAstVisitor> Visitors);