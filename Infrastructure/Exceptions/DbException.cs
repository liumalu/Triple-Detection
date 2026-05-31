using System;

namespace TripleDetection.Infrastructure.Exceptions;

public class DbException : Exception
{
    public DbException(string message, Exception? innerException = null)
        : base(message, innerException) { }
}