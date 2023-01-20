using System;

namespace totlib;

public class TotException : Exception
{
    public TotException(string message) : base(message)
    {
    }
}