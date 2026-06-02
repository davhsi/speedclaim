using Microsoft.AspNetCore.Mvc;

namespace SpeedClaim.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public abstract class BaseApiController : ControllerBase
{
}
