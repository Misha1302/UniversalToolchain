using ArithmeticModule.Core;
using BasicCore.Attributes;

namespace ArithmeticModule.Creators;

[AutoRegisterService]
public class MultiplicationOperationNodeCreator() : BinaryOperationBase("Multiplication");