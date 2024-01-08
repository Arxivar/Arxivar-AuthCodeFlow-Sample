using System.Web;
using System.Web.Mvc;
using Microsoft.Owin.Security.Cookies;
using MvcClientNetFx.Attributes;

namespace MvcClientNetFx.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }


        [OAuthAuthorization]
        public ActionResult Secure() => View();
        
       [AllowAnonymous]
        public ActionResult AuthenticationError(string errorMessage)
        {
            // WARNING: this is a test example, never write in page arbitrary content from unauthenticated request.
            // You could decode error parameters and write a custom message.     
            ViewBag.ErrorMessage = errorMessage;
            return View();
        }
        
        public ActionResult Logout()
        {
            HttpContext.GetOwinContext().Authentication.SignOut(CookieAuthenticationDefaults.AuthenticationType);
            return RedirectToAction("Index");
        }
    }
}