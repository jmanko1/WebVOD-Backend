namespace WebVOD_Backend.Exceptions;

public class RequestErrorException : Exception
{
    public RequestErrorException(int code, string message = "") : base(message)
    {
        StatusCode = code;
    }

    public int StatusCode { get; set; }
}
