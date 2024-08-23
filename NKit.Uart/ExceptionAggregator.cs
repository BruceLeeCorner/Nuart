using System;
using System.Collections.Generic;
using System.Text;

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