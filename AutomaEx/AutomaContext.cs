using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace AutomaEx
{
    /// <summary>
    /// AutomaContext
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public abstract class AutomaContext : IDisposable
    {
        readonly Func<AutomaContext, Automa> _automaFactory;
        /// <summary>
        /// The _logger
        /// </summary>
        protected Action<string> _logger = x => { };

        /// <summary>
        /// Initializes a new instance of the <see cref="AutomaContext" /> class.
        /// </summary>
        /// <param name="automaFactory">The context factory.</param>
        public AutomaContext(Func<AutomaContext, Automa> automaFactory = null)
        {
            _automaFactory = automaFactory;
            Cookies = new CookieCollection();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() =>
            DisposeAutoma();

        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        /// <value>The logger.</value>
        /// <exception cref="ArgumentNullException">value</exception>
        public Action<string> Logger
        {
            get => _logger;
            set => _logger = value ?? throw new ArgumentNullException("value");
        }

        #region Automa

        /// <summary>
        /// Gets the automa.
        /// </summary>
        /// <value>The automa.</value>
        public IAutoma Automa { get; private set; }

        /// <summary>
        /// Ensures the automa.
        /// </summary>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        protected bool EnsureAutoma()
        {
            if (_automaFactory == null)
                return false;
            if (Automa == null)
                Automa = _automaFactory(this);
            return true;
        }

        /// <summary>
        /// Disposes the automa.
        /// </summary>
        protected void DisposeAutoma()
        {
            if (_automaFactory == null)
                return;
            if (Automa == null)
                return;
            Cookies = Automa.Cookies;
            Automa.Dispose();
            Automa = null;
        }

        #endregion

        #region Credentials

        /// <summary>
        /// Gets or sets the service login.
        /// </summary>
        /// <value>The service login.</value>
        public string ServiceLogin { get; set; }

        /// <summary>
        /// Gets or sets the service password.
        /// </summary>
        /// <value>The service password.</value>
        public string ServicePassword { get; set; }

        /// <summary>
        /// Gets or sets the service credential.
        /// </summary>
        /// <value>The service credential.</value>
        public string ServiceCredential { get; set; }

        /// <summary>
        /// Ensures the service login and password.
        /// </summary>
        /// <exception cref="InvalidOperationException">ServiceCredential or ServiceLogin and ServicePassword are required for this operation.</exception>
        /// <exception cref="System.InvalidOperationException">ServiceCredential or, ServiceLogin and ServicePassword are required for this operation.</exception>
        protected void EnsureServiceLoginAndPassword()
        {
            if (string.IsNullOrEmpty(ServiceCredential) && (string.IsNullOrEmpty(ServiceLogin) || string.IsNullOrEmpty(ServicePassword)))
                throw new InvalidOperationException("ServiceCredential or ServiceLogin and ServicePassword are required for this operation.");
        }

        /// <summary>
        /// Gets the network credential.
        /// </summary>
        /// <returns>NetworkCredential.</returns>
        /// <exception cref="InvalidOperationException">Unable to read credential store</exception>
        public virtual NetworkCredential GetNetworkCredential()
        {
            if (string.IsNullOrEmpty(ServiceCredential))
                return new NetworkCredential { UserName = ServiceLogin, Password = ServicePassword };
            if (CredentialManagerEx.Read(ServiceCredential, CredentialManagerEx.CredentialType.GENERIC, out var credential) != 0)
                throw new InvalidOperationException("Unable to read credential store");
            return new NetworkCredential { UserName = credential.UserName, Password = credential.CredentialBlob };
        }

        /// <summary>
        /// Gets the certificate.
        /// </summary>
        /// <param name="thumbprint">The thumbprint.</param>
        /// <param name="storeName">Name of the store.</param>
        /// <param name="location">The location.</param>
        /// <returns>X509Certificate2.</returns>
        public virtual X509Certificate2 GetCertificate(string thumbprint, string storeName = "MY", StoreLocation location = StoreLocation.LocalMachine)
        {
            var store = new X509Store(storeName, location);
            store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
            var x509Certificate = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false)[0];
            store.Close();
            return x509Certificate;
        }

        #endregion

        #region AccessToken

        /// <summary>
        /// Gets or sets the access token.
        /// </summary>
        /// <value>The access token.</value>
        public virtual string AccessToken { get; set; }

        /// <summary>
        /// Gets or sets the access token writer.
        /// </summary>
        /// <value>The access token writer.</value>
        public Action<string> AccessTokenWriter { get; set; }

        /// <summary>
        /// Accesses the token flush.
        /// </summary>
        public void AccessTokenFlush() =>
            AccessTokenWriter?.Invoke(AccessToken);

        #endregion

        #region Cookies

        /// <summary>
        /// Gets or sets the cookies.
        /// </summary>
        /// <value>The cookies.</value>
        public CookieCollection Cookies { get; set; }

        class CookieShim
        {
            public string Name { get; set; }
            public string Value { get; set; }
            public string Domain { get; set; }
            public string Path { get; set; }
            public DateTime Expires { get; set; }
        }

        /// <summary>
        /// Gets or sets the cookies as string.
        /// </summary>
        /// <value>The cookies as string.</value>
        public string CookiesAsString
        {
            get
            {
                if (Cookies == null || Cookies.Count == 0)
                    return string.Empty;
                return JsonConvert.SerializeObject(Cookies.Cast<Cookie>().Select(x =>
                    new CookieShim { Name = x.Name, Value = x.Value, Domain = x.Domain, Path = x.Path, Expires = x.Expires })
                    .ToArray());
            }
            set
            {
                Cookies = new CookieCollection();
                if (string.IsNullOrEmpty(value))
                    return;
                foreach (var x in JsonConvert.DeserializeObject<CookieShim[]>(value).Select(x =>
                    new Cookie(x.Name, x.Value, x.Path, x.Domain) { Expires = x.Expires }))
                    Cookies.Add(x);
            }
        }

        /// <summary>
        /// Gets or sets the cookies writer.
        /// </summary>
        /// <value>The cookies writer.</value>
        public Action<string> CookiesWriter { get; set; }

        /// <summary>
        /// Cookieses the flush.
        /// </summary>
        public void CookiesFlush() =>
            CookiesWriter?.Invoke(CookiesAsString);

        #endregion

        #region TryMethods
        // http://stackoverflow.com/questions/2658908/why-is-targetinvocationexception-treated-as-uncaught-by-the-ide

        /// <summary>
        /// AccessMethod
        /// </summary>
        public enum AccessMethod
        {
            /// <summary>
            /// The try function
            /// </summary>
            TryFunc,
            /// <summary>
            /// The try action
            /// </summary>
            TryAction,
            /// <summary>
            /// The try pager
            /// </summary>
            TryPager,
        }

        /// <summary>
        /// AccessMode
        /// </summary>
        public enum AccessMode
        {
            /// <summary>
            /// The preamble
            /// </summary>
            Preamble,
            /// <summary>
            /// The request
            /// </summary>
            Request,
            /// <summary>
            /// The exception
            /// </summary>
            Exception,
        }

        /// <summary>
        /// Ensures the access.
        /// </summary>
        /// <param name="method">The method.</param>
        /// <param name="mode">The mode.</param>
        /// <param name="tag">The tag.</param>
        /// <param name="value">The value.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        protected virtual bool EnsureAccess(AccessMethod method, AccessMode mode, ref object tag, object value = null) =>
            true;

        /// <summary>
        /// Tries the login.
        /// </summary>
        /// <param name="closeAfter">if set to <c>true</c> [close after].</param>
        /// <param name="tag">The tag.</param>
        /// <param name="loginTimeoutInSeconds">The login timeout in seconds.</param>
        protected virtual void TryLogin(bool closeAfter = true, object tag = null, decimal loginTimeoutInSeconds = -1M)
        {
            if (EnsureAutoma())
                AutomaLogin(closeAfter, tag, loginTimeoutInSeconds);
        }

        /// <summary>
        /// Automas the login.
        /// </summary>
        /// <param name="closeAfter">if set to <c>true</c> [close after].</param>
        /// <param name="tag">The tag.</param>
        /// <param name="loginTimeoutInSeconds">The login timeout in seconds.</param>
        protected virtual void AutomaLogin(bool closeAfter = true, object tag = null, decimal loginTimeoutInSeconds = -1M)
        {
            _logger("AutomaContext::Login");
            Automa.Login(tag, loginTimeoutInSeconds);
            Cookies = Automa.Cookies;
            CookiesFlush();
            if (closeAfter)
                DisposeAutoma();
            _logger("AutomaContext::Done");
        }

        /// <summary>
        /// Tries the function.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action">The action.</param>
        /// <param name="closeAfter">if set to <c>true</c> [close after].</param>
        /// <param name="tag">The tag.</param>
        /// <param name="loginTimeoutInSeconds">The login timeout in seconds.</param>
        /// <returns>T.</returns>
        /// <exception cref="ArgumentNullException">action</exception>
        /// <exception cref="System.ArgumentNullException">action</exception>
        public virtual T TryFunc<T>(Func<T> action, bool closeAfter = true, object tag = null, decimal loginTimeoutInSeconds = -1M)
        {
            if (action == null)
                throw new ArgumentNullException("action");
            EnsureAccess(AccessMethod.TryFunc, AccessMode.Preamble, ref tag);
            var value = action();
            if (value == null)
                return default(T);
            if (EnsureAccess(AccessMethod.TryFunc, AccessMode.Request, ref tag, value))
            {
                TryLogin(closeAfter, tag, loginTimeoutInSeconds);
                return action();
            }
            return value;
        }

        /// <summary>
        /// Tries the function.
        /// </summary>
        /// <typeparam name="T1">The type of the 1.</typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="action">The action.</param>
        /// <param name="t1">The t1.</param>
        /// <param name="closeAfter">if set to <c>true</c> [close after].</param>
        /// <param name="tag">The tag.</param>
        /// <param name="loginTimeoutInSeconds">The login timeout in seconds.</param>
        /// <returns>T.</returns>
        /// <exception cref="ArgumentNullException">action</exception>
        /// <exception cref="System.ArgumentNullException">action</exception>
        public virtual T TryFunc<T1, T>(Func<T1, T> action, T1 t1, bool closeAfter = true, object tag = null, decimal loginTimeoutInSeconds = -1M)
        {
            if (action == null)
                throw new ArgumentNullException("action");
            EnsureAccess(AccessMethod.TryFunc, AccessMode.Preamble, ref tag);
            var value = action(t1);
            if (value == null)
                return default(T);
            if (EnsureAccess(AccessMethod.TryFunc, AccessMode.Request, ref tag, value))
            {
                TryLogin(closeAfter, tag, loginTimeoutInSeconds);
                return action(t1);
            }
            return value;
        }

        /// <summary>
        /// Tries the function.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="exceptionType">Type of the exception.</param>
        /// <param name="action">The action.</param>
        /// <param name="closeAfter">if set to <c>true</c> [close after].</param>
        /// <param name="tag">The tag.</param>
        /// <param name="loginTimeoutInSeconds">The login timeout in seconds.</param>
        /// <returns>T.</returns>
        /// <exception cref="ArgumentNullException">action</exception>
        /// <exception cref="System.ArgumentNullException">action</exception>
        public virtual T TryFunc<T>(Type exceptionType, Func<T> action, bool closeAfter = true, object tag = null, decimal loginTimeoutInSeconds = -1M)
        {
            if (exceptionType == null)
                exceptionType = typeof(Exception);
            if (action == null)
                throw new ArgumentNullException("action");
            EnsureAccess(AccessMethod.TryFunc, AccessMode.Preamble, ref tag);
            try { return action(); }
            catch (Exception e)
            {
                if (exceptionType.IsAssignableFrom(e.GetType()) && EnsureAccess(AccessMethod.TryFunc, AccessMode.Exception, ref tag, e.Message))
                {
                    TryLogin(closeAfter, tag, loginTimeoutInSeconds);
                    return action();
                }
                else throw e;
            }
        }
        /// <summary>
        /// Tries the function.
        /// </summary>
        /// <typeparam name="TException">The type of the t exception.</typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="action">The action.</param>
        /// <param name="closeAfter">if set to <c>true</c> [close after].</param>
        /// <param name="tag">The tag.</param>
        /// <param name="loginTimeoutInSeconds">The login timeout in seconds.</param>
        /// <returns>T.</returns>
        public T TryFunc<TException, T>(Func<T> action, bool closeAfter = true, object tag = null, decimal loginTimeoutInSeconds = -1M) where TException : Exception { return TryFunc(typeof(TException), action, closeAfter, tag, loginTimeoutInSeconds); }


        /// <summary>
        /// Tries the function.
        /// </summary>
        /// <typeparam name="T1">The type of the 1.</typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="exceptionType">Type of the exception.</param>
        /// <param name="action">The action.</param>
        /// <param name="t1">The t1.</param>
        /// <param name="closeAfter">if set to <c>true</c> [close after].</param>
        /// <param name="tag">The tag.</param>
        /// <param name="loginTimeoutInSeconds">The login timeout in seconds.</param>
        /// <returns>T.</returns>
        /// <exception cref="ArgumentNullException">action</exception>
        /// <exception cref="System.ArgumentNullException">action</exception>
        public virtual T TryFunc<T1, T>(Type exceptionType, Func<T1, T> action, T1 t1, bool closeAfter = true, object tag = null, decimal loginTimeoutInSeconds = -1M)
        {
            if (exceptionType == null)
                exceptionType = typeof(Exception);
            if (action == null)
                throw new ArgumentNullException("action");
            EnsureAccess(AccessMethod.TryFunc, AccessMode.Preamble, ref tag);
            try { return action(t1); }
            catch (Exception e)
            {
                if (exceptionType.IsAssignableFrom(e.GetType()) && EnsureAccess(AccessMethod.TryFunc, AccessMode.Exception, ref tag, e.Message))
                {
                    TryLogin(closeAfter, tag, loginTimeoutInSeconds);
                    return action(t1);
                }
                else throw e;
            }
        }
        /// <summary>
        /// Tries the function.
        /// </summary>
        /// <typeparam name="TException">The type of the t exception.</typeparam>
        /// <typeparam name="T1">The type of the t1.</typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="action">The action.</param>
        /// <param name="t1">The t1.</param>
        /// <param name="closeAfter">if set to <c>true</c> [close after].</param>
        /// <param name="tag">The tag.</param>
        /// <param name="loginTimeoutInSeconds">The login timeout in seconds.</param>
        /// <returns>T.</returns>
        public T TryFunc<TException, T1, T>(Func<T1, T> action, T1 t1, bool closeAfter = true, object tag = null, decimal loginTimeoutInSeconds = -1M) where TException : Exception { return TryFunc(typeof(TException), action, t1, closeAfter, tag, loginTimeoutInSeconds); }

        /// <summary>
        /// Tries the action.
        /// </summary>
        /// <param name="exceptionType">Type of the exception.</param>
        /// <param name="action">The action.</param>
        /// <param name="closeAfter">if set to <c>true</c> [close after].</param>
        /// <param name="tag">The tag.</param>
        /// <param name="loginTimeoutInSeconds">The login timeout in seconds.</param>
        /// <exception cref="ArgumentNullException">action</exception>
        /// <exception cref="System.ArgumentNullException">action</exception>
        public virtual void TryAction(Type exceptionType, Action action, bool closeAfter = true, object tag = null, decimal loginTimeoutInSeconds = -1M)
        {
            if (exceptionType == null)
                exceptionType = typeof(Exception);
            if (action == null)
                throw new ArgumentNullException("action");
            EnsureAccess(AccessMethod.TryAction, AccessMode.Preamble, ref tag);
            try { action(); }
            catch (Exception e)
            {
                if (exceptionType.IsAssignableFrom(e.GetType()) && EnsureAccess(AccessMethod.TryAction, AccessMode.Exception, ref tag, e.Message))
                {
                    TryLogin(closeAfter, tag, loginTimeoutInSeconds);
                    action();
                }
                else throw e;
            }
        }
        /// <summary>
        /// Tries the action.
        /// </summary>
        /// <typeparam name="TException">The type of the t exception.</typeparam>
        /// <param name="action">The action.</param>
        /// <param name="closeAfter">if set to <c>true</c> [close after].</param>
        /// <param name="tag">The tag.</param>
        /// <param name="loginTimeoutInSeconds">The login timeout in seconds.</param>
        public void TryAction<TException>(Action action, bool closeAfter = true, object tag = null, decimal loginTimeoutInSeconds = -1M) where TException : Exception { TryAction(typeof(TException), action, closeAfter, tag, loginTimeoutInSeconds); }

        /// <summary>
        /// Tries the action.
        /// </summary>
        /// <typeparam name="T1">The type of the 1.</typeparam>
        /// <param name="exceptionType">Type of the exception.</param>
        /// <param name="action">The action.</param>
        /// <param name="t1">The t1.</param>
        /// <param name="closeAfter">if set to <c>true</c> [close after].</param>
        /// <param name="tag">The tag.</param>
        /// <param name="loginTimeoutInSeconds">The login timeout in seconds.</param>
        /// <exception cref="ArgumentNullException">action</exception>
        /// <exception cref="System.ArgumentNullException">action</exception>
        public virtual void TryAction<T1>(Type exceptionType, Action<T1> action, T1 t1, bool closeAfter = true, object tag = null, decimal loginTimeoutInSeconds = -1M)
        {
            if (exceptionType == null)
                exceptionType = typeof(Exception);
            if (action == null)
                throw new ArgumentNullException("action");
            EnsureAccess(AccessMethod.TryAction, AccessMode.Preamble, ref tag);
            try { action(t1); }
            catch (Exception e)
            {
                if (exceptionType.IsAssignableFrom(e.GetType()) && EnsureAccess(AccessMethod.TryAction, AccessMode.Exception, ref tag, e.Message))
                {
                    TryLogin(closeAfter, tag, loginTimeoutInSeconds);
                    action(t1);
                }
                else throw e;
            }
        }
        /// <summary>
        /// Tries the action.
        /// </summary>
        /// <typeparam name="TException">The type of the t exception.</typeparam>
        /// <typeparam name="T1">The type of the t1.</typeparam>
        /// <param name="action">The action.</param>
        /// <param name="t1">The t1.</param>
        /// <param name="closeAfter">if set to <c>true</c> [close after].</param>
        /// <param name="tag">The tag.</param>
        /// <param name="loginTimeoutInSeconds">The login timeout in seconds.</param>
        public void TryAction<TException, T1>(Action<T1> action, T1 t1, bool closeAfter = true, object tag = null, decimal loginTimeoutInSeconds = -1M) where TException : Exception { TryAction(typeof(TException), action, t1, closeAfter, tag, loginTimeoutInSeconds); }

        /// <summary>
        /// Tries the pager function.
        /// </summary>
        /// <typeparam name="TCursor">The type of the cursor.</typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="action">The action.</param>
        /// <param name="cursor">The cursor.</param>
        /// <param name="nextCursor">The next cursor.</param>
        /// <param name="closeAfter">if set to <c>true</c> [close after].</param>
        /// <param name="tag">The tag.</param>
        /// <param name="loginTimeoutInSeconds">The login timeout in seconds.</param>
        /// <returns>IEnumerable&lt;T&gt;.</returns>
        /// <exception cref="ArgumentNullException">action
        /// or
        /// nextCursor</exception>
        /// <exception cref="System.ArgumentNullException">action
        /// or
        /// nextCursor</exception>
        public virtual IEnumerable<T> TryPagerFunc<TCursor, T>(Func<TCursor, T> action, TCursor cursor, Func<TCursor, T, TCursor> nextCursor, bool closeAfter = true, object tag = null, decimal loginTimeoutInSeconds = -1M)
        {
            if (action == null)
                throw new ArgumentNullException("action");
            if (nextCursor == null)
                throw new ArgumentNullException("nextCursor");
            EnsureAccess(AccessMethod.TryPager, AccessMode.Preamble, ref tag);
            while (cursor != null)
            {
                var values = action(cursor);
                if (values == null)
                    yield break;
                if (EnsureAccess(AccessMethod.TryPager, AccessMode.Request, ref tag, values))
                {
                    TryLogin(closeAfter, tag, loginTimeoutInSeconds);
                    values = action(cursor);
                }
                cursor = nextCursor(cursor, values);
                yield return values;
            }
        }

        /// <summary>
        /// Tries the pager function.
        /// </summary>
        /// <typeparam name="TCursor">The type of the cursor.</typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="exceptionType">Type of the exception.</param>
        /// <param name="action">The action.</param>
        /// <param name="cursor">The cursor.</param>
        /// <param name="nextCursor">The next cursor.</param>
        /// <param name="closeAfter">if set to <c>true</c> [close after].</param>
        /// <param name="tag">The tag.</param>
        /// <param name="loginTimeoutInSeconds">The login timeout in seconds.</param>
        /// <returns>IEnumerable&lt;T&gt;.</returns>
        /// <exception cref="ArgumentNullException">action
        /// or
        /// nextCursor</exception>
        /// <exception cref="System.ArgumentNullException">action
        /// or
        /// nextCursor</exception>
        public virtual IEnumerable<T> TryPagerFunc<TCursor, T>(Type exceptionType, Func<TCursor, T> action, TCursor cursor, Func<TCursor, T, TCursor> nextCursor, bool closeAfter = true, object tag = null, decimal loginTimeoutInSeconds = -1M)
        {
            if (exceptionType == null)
                exceptionType = typeof(Exception);
            if (action == null)
                throw new ArgumentNullException("action");
            if (nextCursor == null)
                throw new ArgumentNullException("nextCursor");
            EnsureAccess(AccessMethod.TryPager, AccessMode.Preamble, ref tag);
            while (cursor != null)
            {
                var values = default(T);
                try { values = action(cursor); }
                catch (Exception e)
                {
                    if (exceptionType.IsAssignableFrom(e.GetType()) && EnsureAccess(AccessMethod.TryPager, AccessMode.Exception, ref tag, e.Message))
                    {
                        TryLogin(closeAfter, tag, loginTimeoutInSeconds);
                        values = action(cursor);
                    }
                    else throw e;
                }
                cursor = nextCursor(cursor, values);
                yield return values;
            }
        }
        /// <summary>
        /// Tries the pager function.
        /// </summary>
        /// <typeparam name="TException">The type of the t exception.</typeparam>
        /// <typeparam name="TCursor">The type of the t cursor.</typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="action">The action.</param>
        /// <param name="cursor">The cursor.</param>
        /// <param name="nextCursor">The next cursor.</param>
        /// <param name="closeAfter">if set to <c>true</c> [close after].</param>
        /// <param name="tag">The tag.</param>
        /// <param name="loginTimeoutInSeconds">The login timeout in seconds.</param>
        /// <returns>IEnumerable&lt;T&gt;.</returns>
        public IEnumerable<T> TryPagerFunc<TException, TCursor, T>(Func<TCursor, T> action, TCursor cursor, Func<TCursor, T, TCursor> nextCursor, bool closeAfter = true, object tag = null, decimal loginTimeoutInSeconds = -1M) where TException : Exception { return TryPagerFunc(typeof(TException), action, cursor, nextCursor, closeAfter, tag, loginTimeoutInSeconds); }

        #endregion

        #region Download

        /// <summary>
        /// Downloads the preamble.
        /// </summary>
        /// <param name="method">The method.</param>
        /// <param name="url">The URL.</param>
        /// <param name="postData">The post data.</param>
        /// <param name="contentType">Type of the content.</param>
        /// <param name="interceptRequest">The intercept request.</param>
        /// <param name="updateCookies">if set to <c>true</c> [update cookies].</param>
        /// <param name="onError">The on error.</param>
        /// <returns>HttpWebResponse.</returns>
        /// <exception cref="ArgumentOutOfRangeException">method</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">method</exception>
        protected virtual HttpWebResponse DownloadPreamble(HttpMethod method, string url, string postData, string contentType = null, Action<HttpWebRequest> interceptRequest = null, bool updateCookies = false, Action<HttpStatusCode, string> onError = null)
        {
            var cookies = Cookies;
            var rq = (HttpWebRequest)WebRequest.Create(url);
            rq.CookieContainer = new CookieContainer();
            rq.CookieContainer.Add(cookies);
            rq.UserAgent = "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2228.0 Safari/537.36";
            rq.AllowWriteStreamBuffering = true;
            rq.ProtocolVersion = HttpVersion.Version11;
            rq.AllowAutoRedirect = true;
            rq.ContentType = (contentType == null ? "application/x-www-form-urlencoded; charset=UTF-8" : contentType);
            interceptRequest?.Invoke(rq);
            if (method == HttpMethod.Get)
                rq.Method = "GET";
            else if (method == HttpMethod.Post)
            {
                rq.Method = "POST";
                if (!string.IsNullOrEmpty(postData))
                {
                    var send = Encoding.Default.GetBytes(postData);
                    rq.ContentLength = send.Length;
                    using (var s = rq.GetRequestStream())
                        s.Write(send, 0, send.Length);
                }
            }
            else
                throw new ArgumentOutOfRangeException("method", method.ToString());
            try
            {
                var rs = (HttpWebResponse)rq.GetResponse();
                if (updateCookies)
                {
                    this.CookieMerge(rs.Cookies);
                    CookiesFlush();
                }
                return rs;
            }
            catch (WebException e)
            {
                if (onError != null)
                    using (var rs = (HttpWebResponse)e.Response)
                    using (var data = rs.GetResponseStream())
                    using (var r = new StreamReader(data))
                        onError(rs.StatusCode, r.ReadToEnd());
                throw;
            }
        }

        /// <summary>
        /// Downloads the name of the file.
        /// </summary>
        /// <param name="rs">The rs.</param>
        /// <returns>System.String.</returns>
        public virtual string DownloadFileName(HttpWebResponse rs)
        {
            var contentDisposition = rs.GetResponseHeader("content-disposition");
            int filenameIdx, semicolonIdx;
            if (contentDisposition != null && (filenameIdx = contentDisposition.IndexOf("filename=") + 9) > 9)
                return contentDisposition.Substring(filenameIdx, (semicolonIdx = contentDisposition.IndexOf(";", filenameIdx)) > -1 ? semicolonIdx - filenameIdx : contentDisposition.Length - filenameIdx).Replace("\"", "");
            return null;
        }

        /// <summary>
        /// Downloads the data.
        /// </summary>
        /// <param name="method">The method.</param>
        /// <param name="url">The URL.</param>
        /// <param name="postData">The post data.</param>
        /// <param name="contentType">Type of the content.</param>
        /// <param name="interceptRequest">The intercept request.</param>
        /// <param name="updateCookies">if set to <c>true</c> [update cookies].</param>
        /// <param name="onError">The on error.</param>
        /// <returns>System.String.</returns>
        public virtual string DownloadData(HttpMethod method, string url, string postData = null, string contentType = null, Action<HttpWebRequest> interceptRequest = null, bool updateCookies = true, Action<HttpStatusCode, string> onError = null)
        {
            var rs = DownloadPreamble(method, url, postData, contentType, interceptRequest, updateCookies, onError);
            using (var r = new StreamReader(rs.GetResponseStream()))
                return r.ReadToEnd();
        }

        /// <summary>
        /// Downloads the json.
        /// </summary>
        /// <param name="method">The method.</param>
        /// <param name="url">The URL.</param>
        /// <param name="postData">The post data.</param>
        /// <param name="contentType">Type of the content.</param>
        /// <param name="interceptRequest">The intercept request.</param>
        /// <param name="updateCookies">if set to <c>true</c> [update cookies].</param>
        /// <param name="onError">The on error.</param>
        /// <returns>JToken.</returns>
        public JToken DownloadJson(HttpMethod method, string url, string postData = null, string contentType = null, Action<HttpWebRequest> interceptRequest = null, bool updateCookies = true, Action<HttpStatusCode, string> onError = null)
        {
            var d = DownloadData(method, url, postData, contentType, interceptRequest, updateCookies, onError);
            return JToken.Parse(d);
        }

        /// <summary>
        /// Downloads the file.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="method">The method.</param>
        /// <param name="url">The URL.</param>
        /// <param name="postData">The post data.</param>
        /// <param name="contentType">Type of the content.</param>
        /// <param name="interceptRequest">The intercept request.</param>
        /// <param name="interceptResponse">The intercept response.</param>
        /// <param name="updateCookies">if set to <c>true</c> [update cookies].</param>
        /// <param name="onError">The on error.</param>
        /// <returns>System.String.</returns>
        public virtual string DownloadFile(string filePath, HttpMethod method, string url, string postData = null, string contentType = null, Action<HttpWebRequest> interceptRequest = null, Action<Stream, Stream> interceptResponse = null, bool updateCookies = false, Action<HttpStatusCode, string> onError = null)
        {
            var rs = DownloadPreamble(method, url, postData, contentType, interceptRequest, updateCookies, onError);
            var fileName = DownloadFileName(rs);
            if (string.IsNullOrEmpty(fileName))
                using (var r = new StreamReader(rs.GetResponseStream()))
                    return r.ReadToEnd();
            fileName = Path.Combine(filePath, fileName);
            // download file
            var downloaded = false;
            var stream = (FileStream)null;
            try
            {
                stream = new FileStream(fileName, FileMode.Create, FileAccess.Write);
                var buffer = new byte[4096];
                using (var input = rs.GetResponseStream())
                {
                    interceptResponse?.Invoke(stream, input);
                    var size = input.Read(buffer, 0, buffer.Length);
                    while (size > 0)
                    {
                        stream.Write(buffer, 0, size);
                        size = input.Read(buffer, 0, buffer.Length);
                    }
                    downloaded = true;
                }
            }
            finally
            {
                if (stream != null)
                {
                    stream.Flush();
                    stream.Close();
                    stream = null;
                    if (!downloaded)
                        File.Delete(fileName);
                }
            }
            return fileName;
        }

        /// <summary>
        /// Downloads the file.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="method">The method.</param>
        /// <param name="url">The URL.</param>
        /// <param name="postData">The post data.</param>
        /// <param name="contentType">Type of the content.</param>
        /// <param name="interceptRequest">The intercept request.</param>
        /// <param name="interceptResponse">The intercept response.</param>
        /// <param name="updateCookies">if set to <c>true</c> [update cookies].</param>
        /// <param name="onError">The on error.</param>
        /// <returns>System.String.</returns>
        public virtual string DownloadFile(MemoryStream stream, HttpMethod method, string url, string postData = null, string contentType = null, Action<HttpWebRequest> interceptRequest = null, Action<Stream, Stream> interceptResponse = null, bool updateCookies = false, Action<HttpStatusCode, string> onError = null)
        {
            var rs = DownloadPreamble(method, url, postData, contentType, interceptRequest, updateCookies, onError);
            var fileName = DownloadFileName(rs);
            if (string.IsNullOrEmpty(fileName))
                using (var r = new StreamReader(rs.GetResponseStream()))
                    return r.ReadToEnd();
            // download file
            var buffer = new byte[4096];
            using (var input = rs.GetResponseStream())
            {
                interceptResponse?.Invoke(stream, input);
                var size = input.Read(buffer, 0, buffer.Length);
                while (size > 0)
                {
                    stream.Write(buffer, 0, size);
                    size = input.Read(buffer, 0, buffer.Length);
                }
            }
            stream.Position = 0;
            return fileName;
        }

        #endregion
    }
}