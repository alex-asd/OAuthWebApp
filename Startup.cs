using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace OathWebApp
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        //  This method gets called by the runtime. Use this method to add services to the container.
        //  First off, we need to update the ConfigureServices method to register the required authentication services
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();

            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Pages/Login";
            });

            services.AddAuthentication(options =>
            {
                //  Registers the authentication services
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = "Github";
                //  Register both cookie and OAuth authentication handlers
            }).AddCookie()
            .AddOAuth("Github", options =>
            {
                //  It's good practice to keep sensitive information like this hidden even for examples
                //  In this case the data is stored in appsettings.json and will not be visable
                options.ClientId = Configuration["Github:ClientId"];
                options.ClientSecret = Configuration["Github:ClientSecret"];
                options.CallbackPath = new PathString("/signin-oath");

                //  Setting up the endpoints as shown in the API documentation
                options.AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
                options.TokenEndpoint = "https://github.com/login/oauth/access_token";
                options.UserInformationEndpoint = "https://api.github.com/user";

                options.SaveTokens = true;

                //  First we specifying the following ClaimActions 
                //  Which will later be ran on the response
                options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
                options.ClaimActions.MapJsonKey(ClaimTypes.Name, "name");

                options.Events = new OAuthEvents
                {
                    //  Here we call the UserInformationEndpoint and receive a JSON response from GitHub
                    OnCreatingTicket = async context =>
                    {
                        var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);

                        var response = await context.Backchannel.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.HttpContext.RequestAborted);
                        response.EnsureSuccessStatusCode();

                        var user = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

                        //  Calling RunClaimActions passes along the user object
                        context.RunClaimActions(user.RootElement);
                    }
                };
            });
        }

        //  This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapRazorPages();
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller}/{action=Index}/{id?}");
            });
        }
    }
}
