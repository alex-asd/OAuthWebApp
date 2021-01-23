using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace OathWebApp.Controllers
{
    [Route("[controller]/[action]")]
    public class AuthController : Controller
    {
        [HttpGet]
        public IActionResult Login(string returnUrl = "/")
        {
            return Challenge(new AuthenticationProperties() { RedirectUri = returnUrl });
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return Redirect(Url.Content("~/Login"));
        }
    }
}
