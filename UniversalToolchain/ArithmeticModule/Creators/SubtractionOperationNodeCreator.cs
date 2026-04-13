namespace ArithmeticModule.Creators;

[AutoRegisterService]
[ArithmeticModeCompatibility(ArithmeticMode.Universal)]
public class SubtractionOperationNodeCreator() : BinaryOperationBase("Subtraction");