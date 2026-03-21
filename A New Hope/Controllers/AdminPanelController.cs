using Microsoft.AspNetCore.Mvc;

namespace A_New_Hope.Controllers
{
    public class AdminPanelController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
