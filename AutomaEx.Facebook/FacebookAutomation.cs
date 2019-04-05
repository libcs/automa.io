using OpenQA.Selenium;
using System;
using System.Net;

namespace AutomaEx.Facebook
{
    /// <summary>
    /// IFacebookAutomation
    /// </summary>
    public interface IFacebookAutomation : IAutomation
    {
    }

    /// <summary>
    /// FacebookAutomation
    /// </summary>
    public class FacebookAutomation : IFacebookAutomation
    {
        const string FacebookUri = "https://www.facebook.com";
        readonly IAutoma _automa;
        readonly IWebDriver _driver;

        /// <summary>
        /// Initializes a new instance of the <see cref="FacebookAutomation" /> class.
        /// </summary>
        /// <param name="automa">The automa.</param>
        /// <param name="driver">The driver.</param>
        public FacebookAutomation(IAutoma automa, IWebDriver driver)
        {
            _automa = automa;
            _driver = driver;
        }

        /// <summary>
        /// Tries the go to URL.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <exception cref="LoginRequiredException"></exception>
        public void TryGoToUrl(string url)
        {
            _driver.Navigate().GoToUrl(url);
            var title = _driver.Title;
            if (title.Contains("Log in") || title.Contains("Log In"))
                throw new LoginRequiredException();
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
            _driver.Navigate().GoToUrl(FacebookUri);
            //if (cookies == null)
            //    _ctx.Cookies = cookies(null);
            var loginElement = _driver.FindElement(By.Id("email"));
            loginElement.SendKeys(credential.UserName);
            var passwordElement = _driver.FindElement(By.Id("pass"));
            passwordElement.SendKeys(credential.Password);
            //var persistentElement = _driver.FindElement(By.Name("persistent"));
            //if (!persistentElement.Selected)
            //    persistentElement.Click();
            var loginLabel = _driver.FindElement(By.Id("loginbutton"));
            var loginButton = loginLabel.FindElement(By.TagName("input"));
            loginButton.Click();
            var title = _driver.Title;
            if (title.Contains("Log in") || title.Contains("Log In"))
                throw new LoginRequiredException();
            cookies(_automa.Cookies);
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
            TryGoToUrl(url);
            var title = _driver.Title;
            if (!title.Contains("Devices"))
                throw new InvalidOperationException();
            var loginElement = _driver.FindElement(By.Name("user_code"));
            loginElement.SendKeys(userCode);
            var continueButton = _driver.FindElement(By.ClassName("oauth_device_code_continue_button"));
            if (continueButton.Displayed) continueButton.Click();
            else
            {
                var loginButton = _driver.FindElement(By.ClassName("selected"));
                if (loginButton.Displayed) loginButton.Click();
            }
            for (var i = 0; i < 3; i++)
            {
                var pageSource = _driver.PageSource;
                if (pageSource.Contains("Success!"))
                    return;
                var confirmElement = _driver.FindElement(By.Name("__CONFIRM__"));
                if (confirmElement != null)
                    confirmElement.Click();
            }
        }
    }
}
