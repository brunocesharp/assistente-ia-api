using Microsoft.AspNetCore.Mvc;

namespace AssistenteIaApi.Controllers;

[ApiController]
[Route("")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    public ActionResult<string> Get() => Ok("API no ar");
}
