namespace ArithmeticModule.Creators;

[AutoRegisterService]
[ArithmeticModeCompatibility(ArithmeticMode.Universal)]
public class SubstractionOperationNodeCreator() : BinaryOperationBase("Substraction");