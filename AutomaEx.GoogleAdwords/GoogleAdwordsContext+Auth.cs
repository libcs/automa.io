using Google.Api.Ads.Common.Lib;
using System;

namespace AutomaEx.GoogleAdwords
{
    public partial class GoogleAdwordsContext
    {
        /// <summary>
        /// Ensures the access.
        /// </summary>
        /// <param name="method">The method.</param>
        /// <param name="mode">The mode.</param>
        /// <param name="tag">The tag.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        protected override bool EnsureAccess(AccessMethod method, AccessMode mode, ref object tag, object value = null)
        {
            switch (mode)
            {
                //case AccessMode.Exception: return (((string)value).IndexOf("(400) Bad Request") != -1);
                default: return true;
            }
        }

        //public void DoAuth2Authorization()
        //{
        //    // Since we are using a console application, set the callback url to null.
        //    AdWordsUser.Config.OAuth2RedirectUri = null;
        //    var oAuth2Provider = (AdWordsUser.OAuthProvider as AdsOAuthProviderForApplications);
        //    // Get the authorization url.
        //    var authorizationUrl = oAuth2Provider.GetAuthorizationUrl();
        //    Console.WriteLine("Open a fresh web browser and navigate to \n\n{0}\n\n. You will be " +
        //        "prompted to login and then authorize this application to make calls to the " +
        //        "AdWords API. Once approved, you will be presented with an authorization code.",
        //        authorizationUrl);

        //    // Accept the OAuth2 authorization code from the user.
        //    Console.Write("Enter the authorization code :");
        //    var authorizationCode = Console.ReadLine();

        //    // Fetch the access and refresh tokens.
        //    oAuth2Provider.FetchAccessAndRefreshTokens(authorizationCode);
        //}

        /// <summary>
        /// Sets the device access token.
        /// </summary>
        /// <param name="func">The function.</param>
        /// <param name="tag">The tag.</param>
        /// <param name="timeoutInSeconds">The timeout in seconds.</param>
        /// <returns></returns>
        public bool SetDeviceAccessToken(Func<string, object, decimal, string> func, object tag = null, decimal timeoutInSeconds = 30M)
        {
            EnsureAppIdAndSecret();
            AdWordsUser.Config.OAuth2RedirectUri = null;
            var oauth2Provider = (AdWordsUser.OAuthProvider as AdsOAuthProviderForApplications);
            var authorizationUrl = oauth2Provider.GetAuthorizationUrl();
            //
            var authorizationCode = func(authorizationUrl, tag, timeoutInSeconds);
            oauth2Provider.FetchAccessAndRefreshTokens(authorizationCode);
            return false;
        }
    }
}
