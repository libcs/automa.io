using System;
using System.Net;

namespace AutomaEx
{
    /// <summary>
    /// IAutomation
    /// </summary>
    public interface IAutomation
    {
        /// <summary>
        /// Logins the specified cookies.
        /// </summary>
        /// <param name="cookies">The cookies.</param>
        /// <param name="credential">The credential.</param>
        /// <param name="tag">The tag.</param>
        void Login(Func<CookieCollection, CookieCollection> cookies, NetworkCredential credential, object tag = null);

        /// <summary>
        /// Sets the device access token.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="userCode">The user code.</param>
        /// <param name="tag">The tag.</param>
        void SetDeviceAccessToken(string url, string userCode, object tag = null);
    }
}
