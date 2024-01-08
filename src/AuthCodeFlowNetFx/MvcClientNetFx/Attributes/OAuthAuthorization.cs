using System;
using System.Security.Claims;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using Abletech.Arxivar.Authentication.OAuth;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Serilog;

namespace MvcClientNetFx.Attributes
{
    public class OAuthAuthorization : AuthorizeAttribute
    {
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            var requestUrl = httpContext.Request.RawUrl;
            Log.Debug("IsAuthorized url: {RequestUrl} ", requestUrl);

            try
            {
                if (httpContext.User.Identity.AuthenticationType !=  CookieAuthenticationDefaults.AuthenticationType || !httpContext.User.Identity.IsAuthenticated)
                {
                    Log.Warning("Unauthenticated user url: {RequestUrl}", requestUrl);
                    Challenge(httpContext);
                    return false;
                }

                var baseResult = base.AuthorizeCore(httpContext);

                if (!baseResult)
                {
                    Log.Warning("Unauthorized request: {RequestUrl}", requestUrl);
                }

                return baseResult;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "IsAuthorized url: {RequestUrl}", requestUrl);
                throw;
            }
        }

        private void Challenge(HttpContextBase httpContext)
        {
            var queryString = httpContext.Request.QueryString.Count > 0 ? $"?{httpContext.Request.QueryString}" : string.Empty;
            var returnUrl = httpContext.Request.Path + queryString;

            var authenticationProperties = new AuthenticationProperties { RedirectUri = returnUrl };
            httpContext.Request.GetOwinContext().Authentication.Challenge(authenticationProperties, OAuthOptions.DEFAULT_AUTH_TYPE);
        }
    }
}