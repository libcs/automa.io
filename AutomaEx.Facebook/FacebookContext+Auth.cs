using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;

namespace AutomaEx.Facebook
{
    public partial class FacebookContext
    {
        Dictionary<long, string> _accounts;

        void InterceptRequest(HttpWebRequest r) { r.Headers["Authorization"] = $"Bearer {AccessToken}"; }
        void InterceptRequestForAccount(HttpWebRequest r, long id)
        {
            string accessToken;
            if (_accounts == null || !_accounts.TryGetValue(id, out accessToken))
                throw new InvalidOperationException($"Unable to find Account {id} Access Token");
            r.Headers["Authorization"] = $"Bearer {accessToken}";
        }

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
                case AccessMode.Exception:
                    {
                        var valueAsString = ((string)value);
                        return (valueAsString.IndexOf("(400) Bad Request") != -1 || valueAsString.IndexOf("(401) Unauthorized") != -1);
                        //return (valueAsString.IndexOf("(401) Unauthorized") != -1);
                    }
                case AccessMode.Request:
                    {
                        var valueAsString = (value as string);
                        if (valueAsString != null)
                        {
                            if (valueAsString.IndexOf("User does not have page access") != -1)
                                throw new UnauthorizedAccessException("User does not have page access");
                            int loginStart, loginEnd;
                            if ((loginStart = valueAsString.IndexOf("<form id=\"login_form\" action=\"/login.php?login_attempt=1&amp;")) > -1 && (loginEnd = valueAsString.IndexOf("</form>", loginStart)) > -1)
                                return true;
                        }
                        return false;
                    }
            }
            return false;
        }

        /// <summary>
        /// Tries the login.
        /// </summary>
        /// <param name="closeAfter">if set to <c>true</c> [close after].</param>
        /// <param name="tag">The tag.</param>
        /// <param name="loginTimeoutInSeconds">The login timeout in seconds.</param>
        protected override void TryLogin(bool closeAfter = true, object tag = null, decimal loginTimeoutInSeconds = 30)
        {
            if (EnsureAutoma())
            {
                //if (tag != null)
                //{
                AutomaLogin(false, tag, loginTimeoutInSeconds);
                SetDeviceAccessToken(RequestedScope, Automa.SetDeviceAccessToken, tag, loginTimeoutInSeconds);
                SetExtendedAccessToken();
                AccessTokenFlush();
                DisposeAutoma();
                //}
                //else base.TryLogin(closeAfter, tag, loginTimeoutInSeconds);
            }
        }

        /// <summary>
        /// Gets me.
        /// </summary>
        /// <returns></returns>
        public dynamic GetMe()
        {
            var r = TryFunc(typeof(WebException), () => DownloadJson(HttpMethod.Get, $"{BASEv}/me?fields=id,name", interceptRequest: InterceptRequest), tag: true);
            return new
            {
                id = (string)r["id"],
                name = (string)r["name"],
            };
        }

        /// <summary>
        /// Loads me accounts.
        /// </summary>
        public void LoadMeAccounts()
        {
            var r = TryFunc(typeof(WebException), () => DownloadJson(HttpMethod.Get, $"{BASEv}/me/accounts?fields=id,name,access_token", interceptRequest: InterceptRequest), tag: true);
            _accounts = new Dictionary<long, string>();
            foreach (JObject i in (JArray)r["data"])
                _accounts.Add(long.Parse((string)i["id"]), (string)i["access_token"]);
        }

        /// <summary>
        /// Sets the device access token.
        /// </summary>
        /// <param name="scope">The scope.</param>
        /// <param name="action">The action.</param>
        /// <param name="tag">The tag.</param>
        /// <param name="timeoutInSeconds">The timeout in seconds.</param>
        /// <returns></returns>
        public bool SetDeviceAccessToken(string scope, Action<string, string, object, decimal> action, object tag = null, decimal timeoutInSeconds = 30M)
        {
            EnsureAppIdAndToken();
            var r = DownloadJson(HttpMethod.Post, $"{BASEv}/device/login?access_token={AppId}|{ClientToken}&scope={scope}");
            var code = (string)r["code"];
            var user_code = (string)r["user_code"];
            var verification_uri = (string)r["verification_uri"];
            var expiresInSeconds = (long)r["expires_in"];
            var intervalMilliseconds = (int)((long)r["interval"]) * 1000;
            action(verification_uri, user_code, tag, timeoutInSeconds);

            var polling = true;
            do
            {
                Console.Write(".");
                Thread.Sleep(intervalMilliseconds);
                try
                {
                    var r2 = DownloadJson(HttpMethod.Post, $"{BASEv}/device/login_status?access_token={AppId}|{ClientToken}&code={code}");
                    AccessToken = (string)r2["access_token"];
                    return true;
                }
                catch (Exception e)
                {
                    var message = e.Message.Substring(e.Message.IndexOf(") ") + 2);
                    Console.Write(message);
                    switch (message)
                    {
                        case "authorization_pending": break;
                        case "authorization_declined": polling = false; break;
                        case "slow_down": break;
                        case "code_expired": polling = false; break;
                    }
                }
            }
            while (polling);
            return false;
        }

        ///// <summary>
        ///// Sets the access token by code.
        ///// </summary>
        ///// <param name="redirectUrl">The redirect URL.</param>
        //public void SetAccessTokenByCode(string redirectUrl)
        //{
        //    using (var client = new WebClient())
        //    {
        //        var data = client.OpenRead($"{BASE}/dialog/oath?client_id={AppId}&client_secret={AppSecret}&redirect_uri={redirectUrl}");
        //        var reader = new StreamReader(data);
        //        AccessToken = reader.ReadToEnd().Split('&')[0].Split('=')[1];
        //    }
        //}

        ///// <summary>
        ///// Sets the access token from code.
        ///// </summary>
        ///// <param name="redirectUrl">The redirect URL.</param>
        ///// <param name="code">The code.</param>
        ///// <exception cref="System.ArgumentNullException">code</exception>
        //public void SetAccessTokenFromCode(string redirectUrl, string code)
        //{
        //    if (string.IsNullOrEmpty(code))
        //        throw new ArgumentNullException("code");
        //    using (var client = new WebClient())
        //    {
        //        var data = client.OpenRead($"{BASE}/oauth/access_token?client_id={AppId}&client_secret={AppSecret}&redirect_uri={redirectUrl}&code={code}");
        //        var reader = new StreamReader(data);
        //        AccessToken = reader.ReadToEnd().Split('&')[0].Split('=')[1];
        //    }
        //}

        ///// <summary>
        ///// Sets the application access token.
        ///// </summary>
        ///// <param name="f">The f.</param>
        //public void SetApplicationAccessToken()
        //{
        //    using (var client = new WebClient())
        //    {
        //        var data = client.OpenRead($"{BASE}/oauth/access_token?client_id={AppId}&client_secret={AppSecret}&grant_type=client_credentials");
        //        var r = new StreamReader(data);
        //        AccessToken = r.ReadToEnd().Split('=')[1];
        //    }
        //}

        /// <summary>
        /// Sets the extended access token.
        /// </summary>
        public void SetExtendedAccessToken()
        {
            EnsureAppIdAndSecret();
            var r = DownloadJson(HttpMethod.Get, $"{BASE}/oauth/access_token?grant_type=fb_exchange_token&client_id={AppId}&client_secret={AppSecret}&fb_exchange_token={AccessToken}");
            AccessToken = (string)r["access_token"];
        }
    }
}
