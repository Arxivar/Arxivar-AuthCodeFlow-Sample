using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MvcClient.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [Authorize]
        public IActionResult Secure() => View();


        [AllowAnonymous]
        public IActionResult Error(string message)
        {
            ViewBag.Error = message;
            return View();
        }

        [AllowAnonymous]
        public IActionResult AuthenticationError(string error, string errorDescription)
        {
            // WARNING: this is a test example, never write in page arbitrary content from unauthenticated request.
            // You could decode error parameters and write a custom message.     
            ViewBag.Error = error;
            ViewBag.ErrorDescription = errorDescription;
            return View();
        }


        public async Task<IActionResult> Logout()
        {
            // Only remove local site cookie
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index");
        }
    }
}