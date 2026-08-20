using Microsoft.AspNetCore.Mvc;

namespace CampusCare.Web.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Admin")) return RedirectToAction("Index", "Admin");
                if (User.IsInRole("Manager")) return RedirectToAction("Index", "Manager");
                if (User.IsInRole("Staff")) return RedirectToAction("Index", "Staff");
                return RedirectToAction("Index", "Student");
            }
            return View();
        }
    }
}
