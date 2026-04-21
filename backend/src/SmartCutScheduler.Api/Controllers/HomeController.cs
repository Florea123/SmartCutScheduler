using Microsoft.AspNetCore.Mvc;

namespace SmartCutScheduler.Api.Controllers
{
    [ApiController]
    [Route("/")]
    public class HomeController : ControllerBase
    {
        [HttpGet]
        public IActionResult Index()
        {
            return Ok("SmartCutScheduler API is running.");
        }
    }
}
