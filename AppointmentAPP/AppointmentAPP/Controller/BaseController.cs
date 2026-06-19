using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentAPP.Controller
{
    //[Route("api/[controller]")]
    //[ApiController]
    //public class BaseController : ControllerBase
    //{
    //}
    [ApiController]
    [Produces("application/json")]
    public abstract class BaseController : ControllerBase
    {
    }
}
