using BasicCore.Contracts;

namespace BasicCore.Builtins;

public static class BuiltinIntrinsicSymbols
{
    public static class Core
    {
        public static readonly IntrinsicSymbol LoadConst = new("Core", "LoadConst");
        public static readonly IntrinsicSymbol CallCSharp = new("Core", "CallCSharp");
        public static readonly IntrinsicSymbol CallCSharpCtor = new("Core", "CallCSharpCtor");
        public static readonly IntrinsicSymbol LoadExternal = new("Core", "LoadExternal");
        public static readonly IntrinsicSymbol StoreExternal = new("Core", "StoreExternal");
    }

    public static class Arithmetic
    {
        public static readonly IntrinsicSymbol Add = new("Arithmetic", "Add");
        public static readonly IntrinsicSymbol Subtract = new("Arithmetic", "Subtract");
        public static readonly IntrinsicSymbol Multiply = new("Arithmetic", "Multiply");
        public static readonly IntrinsicSymbol Divide = new("Arithmetic", "Divide");
    }

    public static class Comparison
    {
        public static readonly IntrinsicSymbol Equal = new("Comparison", "Equal");
        public static readonly IntrinsicSymbol NotEqual = new("Comparison", "NotEqual");
        public static readonly IntrinsicSymbol Greater = new("Comparison", "Greater");
        public static readonly IntrinsicSymbol GreaterOrEqual = new("Comparison", "GreaterOrEqual");
        public static readonly IntrinsicSymbol Less = new("Comparison", "Less");
        public static readonly IntrinsicSymbol LessOrEqual = new("Comparison", "LessOrEqual");
    }

    public static class Boolean
    {
        public static readonly IntrinsicSymbol And = new("Boolean", "And");
        public static readonly IntrinsicSymbol Or = new("Boolean", "Or");
        public static readonly IntrinsicSymbol Not = new("Boolean", "Not");
    }

    public static class Storage
    {
        public static readonly IntrinsicSymbol LoadLocal = new("Storage", "LoadLocal");
        public static readonly IntrinsicSymbol StoreLocal = new("Storage", "StoreLocal");
        public static readonly IntrinsicSymbol LoadLocalRef = new("Storage", "LoadLocalRef");
    }
}