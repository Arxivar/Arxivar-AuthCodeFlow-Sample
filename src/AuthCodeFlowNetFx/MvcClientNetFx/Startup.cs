using System;
using System.Linq;
using System.Net.Http;
using System.Security;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Abletech.Arxivar.Authentication.OAuth;
using IdentityModel.Client;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using MvcClientNetFx;
using Owin;
using Serilog;
using Constants = Abletech.Arxivar.Authentication.OAuth.Constants;

[assembly: OwinStartup(typeof(Startup))]

namespace MvcClientNetFx
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }

        private void ConfigureAuth(IAppBuilder app)
        {
            Log.Information("Configure Auth");

            var authServiceBaseUrl = System.Web.Configuration.WebConfigurationManager.AppSettings["AuthServiceBaseUrl"];
            var clientId =  System.Web.Configuration.WebConfigurationManager.AppSettings["ClientId"];
            var clientSecret =  System.Web.Configuration.WebConfigurationManager.AppSettings["ClientSecret"];
            
            app.SetDefaultSignInAsAuthenticationType(OAuthOptions.DEFAULT_AUTH_TYPE);
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = CookieAuthenticationDefaults.AuthenticationType,
                CookieName = "ClientMvcNetFx",
                CookieHttpOnly = true
            });
            app.UseOAuthAuthentication(options =>
            {
                options.AuthenticationType = OAuthOptions.DEFAULT_AUTH_TYPE;
                options.ClientId = clientId;
                options.ClientSecret = clientSecret;
                options.AuthorizationEndpoint = $"{authServiceBaseUrl}/OAuth/Authorize";
                options.TokenEndpoint = $"{authServiceBaseUrl}/OAuth/Token";
                options.ResponseMode = Constants.FormPost;
                options.CallbackPath = new PathString("/oauth/callback");
                options.SignInAsAuthenticationType = CookieAuthenticationDefaults.AuthenticationType;
                options.Events = new OAuthEvents
                {
                    OnCreatingTicket = async context =>
                    {
                        try
                        {
                            using (var authClient = new HttpClient())
                            {
                                Log.Debug("OnCreatingTicket introspect code: {Substring} ...", context.AccessToken.Substring(0, 5));
                                var introspectToken = await authClient.IntrospectTokenAsync(new TokenIntrospectionRequest
                                {
                                    Address = $"{authServiceBaseUrl}/api/auth/introspect",
                                    ClientId = clientId,
                                    ClientSecret = clientSecret,
                                    Token = context.AccessToken,
                                    ClientCredentialStyle = ClientCredentialStyle.PostBody
                                });

                                Log.Debug("OnCreatingTicket introspect done");

                                if (!introspectToken.IsActive)
                                {
                                    throw new SecurityException("Introspect Token is not active");
                                }

                                var claims = introspectToken.Claims;
                                context.Identity.AddClaims(claims);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Exception while creating ticket event: {ExMessage}", ex.Message);
                            context.ErrorMessage = ex.Message;
                        }
                    },
                    OnRedirectToAuthorizationEndpoint = context =>
                    {
                        if (context.AuthRequestType == OAuthRequestType.Authentication)
                        {
                            var redirectUri = context.RedirectUri;

                            var owinContext = context.OwinContext;
                            if (owinContext.Authentication.AuthenticationResponseChallenge.Properties.Dictionary.TryGetValue("scope", out var scopeValue))
                            {
                                var uri = new Uri(redirectUri);
                                var queryString = HttpUtility.ParseQueryString(uri.Query);
                                var currentScope = queryString["scope"];
                                queryString.Set("scope", $"{currentScope} {scopeValue}");

                                var uriBuilder = new UriBuilder(uri)
                                {
                                    Query = queryString.ToString()
                                };

                                redirectUri = uriBuilder.Uri.ToString();
                            }

                            Log.Debug("Authentication code flow starting -> redirect: {RedirectUri}", redirectUri);
                            context.Response.Redirect(redirectUri);
                        }

                        return Task.CompletedTask;
                    },
                    OnRemoteFailure = context =>
                    {
                        if (context.Failure is SecurityException se)
                        {
                            Log.Error(se, "Authentication failed check auth service log -> {SeMessage}", se.Message);

                            var urlHelper = new UrlHelper(HttpContext.Current.Request.RequestContext);

                            var errorMessage = string.Empty;
                            var returnUrl = string.Empty;


                            if (se.Data.Contains("message"))
                            {
                                errorMessage = (se.Data["message"] as string)?.Split(';').FirstOrDefault();
                            }

                            if (se.Data.Contains(".redirect"))
                            {
                                returnUrl = se.Data[".redirect"]?.ToString() ?? string.Empty;
                            }

                            var routeValues = new { errorMessage, returnUrl };


                            var redirectUrl = urlHelper.Action("authenticationError", "Home", routeValues);

                            Log.Information("Redirect authentication to error page: {RedirectUrl}", redirectUrl);
                            context.Response.Redirect(redirectUrl);
                            context.HandleResponse();
                        }

                        return Task.CompletedTask;
                    }
                };
            });
        }
    }
}