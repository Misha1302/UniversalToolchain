namespace ArithmeticModule.Creators;

[AutoRegisterService]
[ArithmeticModeCompatibility(ArithmeticMode.Universal)]
public class DivisionOperationNodeCreator() : BinaryOperationBase("Division");