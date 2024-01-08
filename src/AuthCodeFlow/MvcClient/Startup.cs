using System;
using System.Net.Http;
using System.Threading.Tasks;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace MvcClient
{
    public class Startup
    {
        private IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            services.AddAuthentication(options =>
                {
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = OAuthDefaults.DisplayName;
                })
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options => { options.Cookie.Name = "MvcClient"; })
                .AddOAuth(OAuthDefaults.DisplayName, options =>
                {
                    var arxivarAuthSettings = Configuration.GetSection("ArxivarAuthSettings").Get<ArxivarAuthSettings>();

                    // ARXivar OAuth2 code flow authentication
                    options.ClientId = arxivarAuthSettings.ClientId;
                    options.ClientSecret = arxivarAuthSettings.ClientSecret;
                    options.AuthorizationEndpoint = $"{arxivarAuthSettings.AuthServiceBaseUrl}/OAuth/Authorize";
                    options.TokenEndpoint = $"{arxivarAuthSettings.AuthServiceBaseUrl}/OAuth/Token";
                    options.CallbackPath = "/oauth/callback";
                    options.SaveTokens = true;

                    /*
                    // If SaveTokens is true, ARXivar access token and refresh token are saved in the cookie so they can be retrieved during a call like this:
                    var accessToken = await HttpContext.GetTokenAsync("access_token");
                    var refreshToken = await HttpContext.GetTokenAsync("refresh_token");
                    // Application cookie gets a lot bigger: set SaveTokens = false if you don't need to call ARXivar services from your application.
                    */

                    options.UsePkce = false;
                    options.Events = new OAuthEvents
                    {
                        OnRemoteFailure = context =>
                        {
                            Log.Error(context.Failure, "Authorization error");
                            RemoteFailure(context);
                            context.HandleResponse();
                            return Task.CompletedTask;
                        },
                        OnCreatingTicket = async context =>
                        {
                            Log.Debug("ARXivar access token received: {Token}", context.AccessToken);

                            var c = new HttpClient();

                            var introspectEndPoint = $"{arxivarAuthSettings.AuthServiceBaseUrl}/api/auth/introspect";
                            Log.Debug("Decoding ARXivar access token through introspection endpoint: {EndPoint}", introspectEndPoint);

                            var introspectToken = await c.IntrospectTokenAsync(new TokenIntrospectionRequest
                            {
                                Address = introspectEndPoint,
                                ClientId = arxivarAuthSettings.ClientId,
                                ClientSecret = arxivarAuthSettings.ClientSecret,
                                Token = context.AccessToken,
                                ClientCredentialStyle = ClientCredentialStyle.PostBody
                            });

                            if (!introspectToken.IsActive)
                            {
                                throw new Exception("Token not active");
                            }

                            var claims = introspectToken.Claims;
                            context.Identity?.AddClaims(claims);

                            Log.Information("{Username} has been authorized", context.Identity?.Name);
                        },

                        OnRedirectToAuthorizationEndpoint = context =>
                        {
                            Log.Information("Authorization challenge redirect to: {EndPoint}", context.RedirectUri);
                            context.Response.Redirect(context.RedirectUri);
                            return Task.CompletedTask;
                        }
                    };
                });
        }

        private static void RemoteFailure(RemoteFailureContext context)
        {
            var error = string.Empty;
            var errorDescription = string.Empty;

            if (context.Failure != null && context.Failure.Data.Contains("error"))
            {
                error = System.Web.HttpUtility.UrlEncode(context.Failure.Data["error"]?.ToString());
            }

            if (context.Failure != null && context.Failure.Data.Contains("error_description"))
            {
                errorDescription = System.Web.HttpUtility.UrlEncode(context.Failure.Data["error_description"]?.ToString());
            }

            Log.Error("Authorization error: {Error} Description: {Description}", error, errorDescription);

            var redirectUrl = $"/home/authenticationError?error={error}&errorDescription={errorDescription}";
            context.Response.Redirect(redirectUrl);
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseDeveloperExceptionPage();
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints => { endpoints.MapDefaultControllerRoute(); });
        }
    }
}