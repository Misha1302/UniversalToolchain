using ArithmeticModule.Core;
using BasicCore.Attributes;

namespace ArithmeticModule.Creators;

[AutoRegisterService]
public class DivisionOperationNodeCreator() : BinaryOperationBase("Division");