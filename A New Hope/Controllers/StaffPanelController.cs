using Microsoft.AspNetCore.Mvc;

namespace A_New_Hope.Controllers
{
    public class StaffPanelController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
