// Правила написания кода

// обязательно класть аргументы на стек в том порядке, чтобы можно было вычислить типы. Например:
// a = 5 -> 
// push 5
// push reference to a
// set

// Jmp, JmpIf, JmpIfNot - all branches are Directional

// вызовы C# не поддерживают интерфейсы, но поддерживают методы вида `Add<T>(T a, T b) where T : IAddable<T>`