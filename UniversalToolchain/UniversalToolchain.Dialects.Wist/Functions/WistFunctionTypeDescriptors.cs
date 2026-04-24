using UniversalToolchain.Functions.Abstractions;

namespace UniversalToolchain.Dialects.Wist.Functions;

public static class WistFunctionTypeDescriptors
{
    public static readonly FunctionTypeDescriptor Number = new("number");
    public static readonly FunctionTypeDescriptor Bool = new("bool");
}
