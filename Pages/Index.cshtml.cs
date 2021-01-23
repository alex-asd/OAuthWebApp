using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json.Linq;
using RegCheckService;
using static RegCheckService.CarRegSoapClient;
using System.Text.RegularExpressions;
using Octokit;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace OathWebApp.Pages
{
    [Authorize]
    public class IndexModel : PageModel
    {
        public string GitHubUrl { get; set; }
        public string GitHubLogin { get; set; }

        [BindProperty]
        public string NumberPlate { get; set; }

        //  There are more than one way to access the user's data after authentication
        //  Alternate to this method would be to to simply request it in the Startup class then map it, so we can use it here
        //  In this case, I prefer using Octokit since its' API can provide with a lot more functionality
        public async Task OnGetAsync()
        {
            if (User.Identity.IsAuthenticated)
            {
                var github = new GitHubClient(new ProductHeaderValue("OathWebApp"));
                string accessToken = await HttpContext.GetTokenAsync("access_token");

                if (accessToken != null)
                {
                    github.Credentials = new Credentials(accessToken);

                    var user = await github.User.Current();

                    GitHubLogin = user.Login;
                    GitHubUrl = user.HtmlUrl;
                }
            }
        }

        public async Task OnPostAsync()
        {
            //  Check if input matches the Danish number plate pattern
            //  If it doesn't, simply inform the user of his mistake and exit
            //  To match the pattern I use regular expression
            Regex rgx = new Regex(@"^[A-Za-z]{2}[0-9]{5}\z");
            if (!rgx.IsMatch(NumberPlate))
            {
                ViewData["error"] = "Please enter correct number plate format";
                return;
            }

            try
            {
                //  The "client" variable is our proxy, which will carry out the task
                //  The response is XML which contains some data and "vehicleJson"
                //  The json object is then parsed into a more usable dynamic object, but of course depending on the case we could use Dictionaries, Lists or other types of structures
                CarRegSoapClient client = new CarRegSoapClient(EndpointConfiguration.CarRegSoap);
                var result = await client.CheckDenmarkAsync(NumberPlate, "alexsd");

                var jsonObj = (dynamic)JObject.Parse(result.vehicleJson);

                //  The data can be handled in various ways, but just for demonstration I use ViewData to pass it to the view
                ViewData["vehicleDescription"] = $"{ jsonObj.Description } with { jsonObj.EngineSize.CurrentTextValue }cc { jsonObj.FuelType.CurrentTextValue } engine";
            }
            catch (Exception ex)
            {
                //  Different exceptions may occur, but the most common would be when the web service cannot find any data regarding a number
                //  In which case we will simply display it's message
                ViewData["error"] = ex.Message;
            }
        }
    }
}
