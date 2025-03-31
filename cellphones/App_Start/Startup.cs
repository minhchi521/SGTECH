using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Google;
using Owin;
using System;

[assembly: OwinStartup(typeof(cellphones.Startup))]
namespace cellphones
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Configure the sign-in cookie
            app.SetDefaultSignInAsAuthenticationType("ExternalCookie");

            // Add authentication middleware
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = "ApplicationCookie",
                LoginPath = new PathString("/Account/Index")
            });

            // Add external cookie middleware
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = "ExternalCookie",
                AuthenticationMode = Microsoft.Owin.Security.AuthenticationMode.Passive,
                ExpireTimeSpan = TimeSpan.FromMinutes(5)
            });

            // Add Google authentication
            app.UseGoogleAuthentication(new GoogleOAuth2AuthenticationOptions
            {
                ClientId ="yourID",
                ClientSecret = "YourID",
                SignInAsAuthenticationType = "ExternalCookie"
            });
        }
    }
}