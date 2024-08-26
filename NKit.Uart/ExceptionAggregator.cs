using System;

namespace NKit.Uart
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