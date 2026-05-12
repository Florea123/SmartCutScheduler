using Microsoft.AspNetCore.Mvc;

namespace SmartCutScheduler.Api.Controllers
{
    [ApiController]
    [Route("/")]
    public class HomeController : ControllerBase
    {
        [HttpGet]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        public IActionResult Index()
        {
            return Ok("SmartCutScheduler API is running.");
        }
    }
}
