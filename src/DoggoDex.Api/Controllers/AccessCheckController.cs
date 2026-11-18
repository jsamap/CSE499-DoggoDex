using DoggoDex.Api.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DoggoDex.Api.Controllers;

[ApiController]
[Route("api/access-check")]
public class AccessCheckController : ControllerBase
{
    [HttpGet("owner")]
    [Authorize(Policy = Policies.OwnerOnly)]
    public IActionResult Owner() => Ok(new { message = "You are a Dog Owner." });

    [HttpGet("business")]
    [Authorize(Policy = Policies.BusinessOnly)]
    public IActionResult Business() => Ok(new { message = "You are a Business user." });

    [HttpGet("any")]
    [Authorize(Policy = Policies.AnyUser)]
    public IActionResult Any() => Ok(new { message = "You are signed in." });
}