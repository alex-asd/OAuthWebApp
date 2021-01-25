## Table of contents
* [General info](#general-info)
* [Technologies](#technologies)
* [Connecting to GitHub API](#connecting-to-github-api)
* [Consuming the Web Service](#consuming-the-web-service)

## General info
This project is a .NET Core web application created simply to show basic level of proficiency with OAuth 2.0 and Web Services.
	
## Technologies
Project is created with:
* .NET Core: 3.1
* Razor Pages
* External SOAP API - http://dk.registreringsnummerapi.com/
* Octokit: 0.48.0
* Newtonsoft.Json library version: 12.0.3
	
## Connecting to GitHub API
### Setup
Before anything else, the application must be registered with GitHub's OAuth Apps - [Link](https://github.com/settings/applications/new)
### Configure Startup class
First things first, the ```ConfigureServices()``` method will be changed by registering the required authentication services
```
options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
options.DefaultChallengeScheme = "Github";
```

Next we register the cookie and OAuth handlers by calling respectively ```AddCookie()``` and ```AddOAuth()```
For the Authentication handler we need to specify few parameters provided by the identity provider.
```
options.ClientId = Configuration["Github:ClientId"];
options.ClientSecret = Configuration["Github:ClientSecret"];
options.CallbackPath = new PathString("/signin-oauth");
```
We then specify the endpoints
```
options.AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
options.TokenEndpoint = "https://github.com/login/oauth/access_token";
options.UserInformationEndpoint = "https://api.github.com/user";
```
But the actuall request is made in ```OnCreatingTicket()```, where we call ```UserInformationEndpoint``` to receive a json response from GitHub:
```
{
  "login": "octocat",
  "id": 1,
  "node_id": "MDQ6VXNlcjE=",
  "avatar_url": "https://github.com/images/error/octocat_happy.gif",
  "gravatar_id": "",
  "url": "https://api.github.com/users/octocat",
  "html_url": "https://github.com/octocat",
  "followers_url": "https://api.github.com/users/octocat/followers",
  "following_url": "https://api.github.com/users/octocat/following{/other_user}",
  "gists_url": "https://api.github.com/users/octocat/gists{/gist_id}",
  "starred_url": "https://api.github.com/users/octocat/starred{/owner}{/repo}",
  "subscriptions_url": "https://api.github.com/users/octocat/subscriptions",
  "organizations_url": "https://api.github.com/users/octocat/orgs",
  "repos_url": "https://api.github.com/users/octocat/repos",
  "events_url": "https://api.github.com/users/octocat/events{/privacy}",
  "received_events_url": "https://api.github.com/users/octocat/received_events",
  "type": "User",
  "site_admin": false,
  "name": "monalisa octocat",
  "company": "GitHub",
  "blog": "https://github.com/blog",
  "location": "San Francisco",
  "email": "octocat@github.com",
  "hireable": false,
  "bio": "There once was...",
  "twitter_username": "monatheoctocat",
  "public_repos": 2,
  "public_gists": 1,
  "followers": 20,
  "following": 0,
  "created_at": "2008-01-14T04:33:35Z",
  "updated_at": "2008-01-14T04:33:35Z",
  "private_gists": 81,
  "total_private_repos": 100,
  "owned_private_repos": 100,
  "disk_usage": 10000,
  "collaborators": 8,
  "two_factor_authentication": true,
  "plan": {
    "name": "Medium",
    "space": 400,
    "private_repos": 20,
    "collaborators": 0
  }
}
```
This example was taken from [GitHub Docs](https://docs.github.com/en/rest/reference/users).
And with this response we notify the OAuth authentication handler by specifying ```ClaimActions``` and subsequently calling ```RunClaimActions```

### Getting the user's data
The ```GitHubClient``` uses the generated access token to subsequently return the user's data. All operations are async and we need to await them.
```
if (User.Identity.IsAuthenticated)
{
  var github = new GitHubClient(new ProductHeaderValue("OAuthWebApp"));
  string accessToken = await HttpContext.GetTokenAsync("access_token");

  if (accessToken != null)
  {
    github.Credentials = new Credentials(accessToken);

    var user = await github.User.Current();
    
    GitHubLogin = user.Login;
    GitHubUrl = user.HtmlUrl;
  }
}
```
Octokit is very powerful, for more information on it, check out this [link](https://github.com/octokit) and [Getting started](https://github.com/octokit/octokit.net/blob/main/docs/getting-started.md)

## Consuming the Web Service
### Setup
**Connected services** allows you to create client-side code easily using direct .asmx URI or WSDL file. In this case, both are an option, but i will cover only what I used.

The operations is very straightforward, right-click on **Connected services** -> in my case **WCF Web Service Reference Provider** -> paste the .asmx URI, select the service and give it a proper name. .NET will generate a client proxy class.

### Getting the result
```
CarRegSoapClient client = new CarRegSoapClient(EndpointConfiguration.CarRegSoap);
var result = await client.CheckDenmarkAsync(NumberPlate, "alexsd");

var jsonObj = (dynamic)JObject.Parse(result.vehicleJson);

//  The data can be handled in various ways, but just for demonstration I use ViewData to pass it to the view
ViewData["vehicleDescription"] = $"{ jsonObj.Description } with { jsonObj.EngineSize.CurrentTextValue }cc { jsonObj.FuelType.CurrentTextValue } engine";
```
