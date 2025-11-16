// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

namespace TypeInference;

public class TypeInferenceException : Exception
{
    public TypeInferenceException(string message) : base(message)
    {
    }

    public TypeInferenceException(string message, Exception inner) : base(message, inner)
    {
    }
}