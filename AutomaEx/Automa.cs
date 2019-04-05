using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Net;
using NetCookie = System.Net.Cookie;
using SelCookie = OpenQA.Selenium.Cookie;

namespace AutomaEx
{
    /// <summary>
    /// IAutoma
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public interface IAutoma : IDisposable
    {
        /// <summary>
        /// Gets or sets the cookies.
        /// </summary>
        /// <value>The cookies.</value>
        CookieCollection Cookies { get; set; }
        /// <summary>
        /// Logins this instance.
        /// </summary>
        /// <param name="tag">The tag.</param>
        /// <param name="timeoutInSeconds">The timeout in seconds.</param>
        void Login(object tag = null, decimal timeoutInSeconds = -1M);
        /// <summary>
        /// Sets the device access token.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="code">The code.</param>
        /// <param name="tag">The tag.</param>
        /// <param name="timeoutInSeconds">The timeout in seconds.</param>
        void SetDeviceAccessToken(string url, string code, object tag = null, decimal timeoutInSeconds = -1M);
    }

    /// <summary>
    /// Automa
    /// </summary>
    /// <seealso cref="AutomaEx.IAutoma" />
    public class Automa : IAutoma
    {
        readonly IWebDriver _d;
        readonly IAutomation _automation;
        readonly AutomaContext _connector;
        bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="Automa" /> class.
        /// </summary>
        /// <param name="connector">The connector.</param>
        /// <param name="automationFactory">The automation factory.</param>
        /// <param name="defaultTimeoutIsSeconds">The default timeout is seconds.</param>
        public Automa(AutomaContext connector, Func<IAutoma, IWebDriver, IAutomation> automationFactory, decimal defaultTimeoutIsSeconds = 30M)
        {
            // [http://stackoverflow.com/questions/33265701/protractor-allow-notifications-chrome-46]
            var chromeOptions = new ChromeOptions();
            chromeOptions.AddUserProfilePreference("profile.managed_default_content_settings.notifications", 1);
            _d = new ChromeDriver(AppDomain.CurrentDomain.BaseDirectory, chromeOptions);
            _d.Manage().Timeouts().ImplicitWait = TimeSpan.FromMinutes(5);
            _connector = connector;
            _automation = automationFactory(this, _d);
            DefaultTimeoutIsSeconds = defaultTimeoutIsSeconds;
        }
        /// <summary>
        /// Finalizes an instance of the <see cref="Automa" /> class.
        /// </summary>
        ~Automa() { Dispose(false); }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing) { }
                _d.Quit();
                _d.Dispose();
                _disposed = true;
            }
        }

        /// <summary>
        /// Gets or sets the default timeout is seconds.
        /// </summary>
        /// <value>The default timeout is seconds.</value>
        public decimal DefaultTimeoutIsSeconds { get; set; }

        static string CookieValueEncode(string value) => value.Replace(",", "%2C");
        static string CookieValueDecode(string value) => value.Replace("%2C", ",");

        /// <summary>
        /// Gets the cookies.
        /// </summary>
        /// <value>The cookies.</value>
        /// <exception cref="ArgumentNullException">value</exception>
        /// <exception cref="System.ArgumentNullException">value</exception>
        public CookieCollection Cookies
        {
            get
            {
                var cookies = new CookieCollection();
                foreach (var x in _d.Manage().Cookies.AllCookies)
                    cookies.Add(new NetCookie(x.Name, CookieValueEncode(x.Value), x.Path, x.Domain) { Expires = (x.Expiry != null ? x.Expiry.Value : DateTime.MinValue), HttpOnly = x.IsHttpOnly, Secure = x.Secure });
                return cookies;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                var cookies2 = _d.Manage().Cookies;
                cookies2.DeleteAllCookies();
                foreach (NetCookie x in value)
                    cookies2.AddCookie(new SelCookie(x.Name, CookieValueDecode(x.Value), x.Domain, x.Path, (x.Expires != DateTime.MinValue ? (DateTime?)x.Expires : null)));
            }
        }

        /// <summary>
        /// Logins this instance.
        /// </summary>
        /// <param name="tag">The tag.</param>
        /// <param name="timeoutInSeconds">The timeout in seconds.</param>
        public void Login(object tag = null, decimal timeoutInSeconds = -1M)
        {
            if (timeoutInSeconds == -1M) timeoutInSeconds = DefaultTimeoutIsSeconds;
            Action action = () => _automation.Login(_connector.CookieGetSet, _connector.GetNetworkCredential(), tag);
            if (timeoutInSeconds > 0) action.TimeoutInvoke((int)(timeoutInSeconds * 1000M));
            else action();
        }

        /// <summary>
        /// Sets the device access token.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="code">The code.</param>
        /// <param name="tag">The tag.</param>
        /// <param name="timeoutInSeconds">The timeout in seconds.</param>
        public void SetDeviceAccessToken(string url, string code, object tag = null, decimal timeoutInSeconds = -1M)
        {
            if (timeoutInSeconds == -1M) timeoutInSeconds = DefaultTimeoutIsSeconds;
            Action action = () => TryAction(x => _automation.SetDeviceAccessToken(url, code), tag, timeoutInSeconds);
            if (timeoutInSeconds > 0) action.TimeoutInvoke((int)(timeoutInSeconds * 1000M));
            else action();
        }

        void TryAction(Action<IAutoma> action, object tag = null, decimal timeoutInSeconds = -1M)
        {
            if (timeoutInSeconds == -1M) timeoutInSeconds = DefaultTimeoutIsSeconds;
            try { action(this); }
            catch (LoginRequiredException)
            {
                Login(tag, timeoutInSeconds);
                action(this);
            }
        }

        T TryFunc<T>(Func<IAutoma, T> action, object tag = null, decimal timeoutInSeconds = -1M)
        {
            if (timeoutInSeconds == -1M) timeoutInSeconds = DefaultTimeoutIsSeconds;
            try { return action(this); }
            catch (LoginRequiredException)
            {
                Login(tag, timeoutInSeconds);
                return action(this);
            }
        }
    }
}