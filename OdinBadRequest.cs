using Cola.CoreUtils.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Cola.ColaWebApi;

public class OdinBadRequest : BadRequestObjectResult
{
    public OdinBadRequest(string errorCode, string message) : base(new ErrorModel(errorCode, message))
    {
        StatusCode = errorCode.ToInt();
    }
}