using System;

namespace RedLangCompiler.Exceptions;

public class CompilationException : Exception
{
    public int Line { get; }
    public int Column { get; }

    public CompilationException(string message, int line = 0, int column = 0, Exception? innerException = null)
        : base(message, innerException)
    {
        Line = line;
        Column = column;
    }
}
