using OpenQA.Selenium;
using System;
using System.Net;

namespace AutomaEx.GoogleAdwords
{
    /// <summary>
    /// IGoogleAdwordsAutomation
    /// </summary>
    public interface IGoogleAdwordsAutomation : IAutomation
    {
    }

    /// <summary>
    /// GoogleAdwordsAutomation
    /// </summary>
    public class GoogleAdwordsAutomation : IGoogleAdwordsAutomation
    {
        const string FacebookUri = "https://www.google.com";
        readonly IAutoma _automa;
        readonly IWebDriver _driver;

        /// <summary>
        /// Initializes a new instance of the <see cref="GoogleAdwordsAutomation"/> class.
        /// </summary>
        /// <param name="automa">The automa.</param>
        /// <param name="driver">The driver.</param>
        public GoogleAdwordsAutomation(IAutoma automa, IWebDriver driver)
        {
            _automa = automa;
            _driver = driver;
        }

        /// <summary>
        /// Logins the specified cookies.
        /// </summary>
        /// <param name="cookies">The cookies.</param>
        /// <param name="credential">The credential.</param>
        /// <param name="tag">The tag.</param>
        /// <exception cref="LoginRequiredException"></exception>
        public void Login(Func<CookieCollection, CookieCollection> cookies, NetworkCredential credential, object tag)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Sets the device access token.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="userCode">The code.</param>
        /// <param name="tag">The tag.</param>
        /// <exception cref="System.NotSupportedException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public void SetDeviceAccessToken(string url, string userCode, object tag)
        {
            throw new NotSupportedException();
        }
    }
}
