using System;

namespace Nuart
{
    public class Response
    {
        public Response()
        {
            ErrorMsg = string.Empty;
            Exception = null;
        }

        public Response(string errorMsg)
        {
            ErrorMsg = errorMsg;
            Exception = null;
        }

        public Response(Exception ex)
        {
            Exception = ex;
            ErrorMsg = string.Empty;
        }

        public Response(Exception ex, string errorMsg)
        {
            ErrorMsg = errorMsg;
            Exception = ex;
        }

        public string ErrorMsg { get; }
        public Exception Exception { get; }
        public bool IsSuccess => Exception == null && string.IsNullOrWhiteSpace(ErrorMsg);
    }

    public class Response<T> : Response
    {
        public Response(T data)
        {
            Data = data;
        }

        public Response(T data, string errorMsg) : base(errorMsg)
        {
            Data = data;
        }

        public Response(T data, Exception ex) : base(ex)
        {
            Data = data;
        }

        public Response(T data, Exception ex, string errorMsg) : base(ex, errorMsg)
        {
            Data = data;
        }

        public T Data { get; }
    }
}