namespace ArithmeticModule.Creators;

[AutoRegisterService]
[ArithmeticModeCompatibility(ArithmeticMode.Universal)]
public class MultiplicationOperationNodeCreator() : BinaryOperationBase("Multiplication");