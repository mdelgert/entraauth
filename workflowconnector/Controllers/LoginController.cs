using Microsoft.AspNetCore.Mvc;

namespace workflowconnector.Controllers
{
    public class LoginController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
