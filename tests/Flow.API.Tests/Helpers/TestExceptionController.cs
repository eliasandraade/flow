using Microsoft.AspNetCore.Mvc;

namespace Flow.API.Tests.Helpers;

[ApiController]
[Route("test-exception")]
public class TestExceptionController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        var ex = ExceptionMiddlewareTestEndpoints.NextException;
        if (ex is not null) throw ex;
        return Ok();
    }
}
