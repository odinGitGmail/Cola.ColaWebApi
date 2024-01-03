namespace Cola.ColaWebApi;

public class ErrorModel
{
    public string ErrorCode { get; set; }
    public string ErrorMessage { get; set; }

    public ErrorModel(string errorCode, string errorMessage)
    {
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }
}