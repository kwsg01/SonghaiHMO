using Microsoft.AspNetCore.Mvc;

namespace SonghaiHMO.Web.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Portal()
        {
            return View();
        }

        public IActionResult Login(string role)
        {
            ViewBag.Role = role ?? "admin";
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }
    }
}