namespace ArithmeticModule.Creators;

[AutoRegisterService]
[ArithmeticModeCompatibility(ArithmeticMode.Universal)]
public class TextualAdditionOperationNodeCreator() : BinaryOperationBase("TextualAddition");
