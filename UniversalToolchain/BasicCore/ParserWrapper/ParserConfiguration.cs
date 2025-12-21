namespace BasicCore.ParserWrapper;

public record ParserConfiguration(LevelCollection<float, IAstNodeCreator> NodeCreators);