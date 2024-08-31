using System;

namespace Nuart
{
    public class MissingMatchException : Exception
    {
        public MissingMatchException(string message)
        {
            Message = message;
        }

        public override string Message { get; }
    }
}