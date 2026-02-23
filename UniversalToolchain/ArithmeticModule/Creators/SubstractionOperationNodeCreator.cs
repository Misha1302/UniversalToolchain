using ArithmeticModule.Core;
using BasicCore.Attributes;

namespace ArithmeticModule.Creators;

[AutoRegisterService]
public class SubstractionOperationNodeCreator() : BinaryOperationBase("Substraction");