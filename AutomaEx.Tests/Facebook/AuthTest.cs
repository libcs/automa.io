using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AutomaEx.Facebook;
using System.IO;
using System.Net.Http;

namespace AutomaEx.Tests.Facebook
{
    [TestClass]
    public class AuthTest
    {
        #region Preamble

        const string SyncPath = "C:\\T_";
        const string CookieFile = "cookies.json";
        static FacebookContext _ctx = null;

        [ClassInitialize]
        public static void ClassInitialize(TestContext ctx)
        {
            var syncPath = SyncPath;
            if (!Directory.Exists(syncPath))
                Directory.CreateDirectory(syncPath);
            var cookieFile = Path.Combine(syncPath, CookieFile);
            _ctx = new FacebookContext
            {
                Logger = (x) => Console.WriteLine(x),
                CookiesAsString = (File.Exists(cookieFile) ? File.ReadAllText(cookieFile) : null),
                CookiesWriter = (x) => File.WriteAllText(cookieFile, x),
                ServiceCredential = "Facebook",
                AppId = "718092791654479",
                AppSecret = "c2bcfc113b46c1ba81c3ef5a6234945f",
            };
        }

        [ClassCleanup]
        public static void ClassCleanup() { if (_ctx != null) _ctx.Dispose(); _ctx = null; }

        #endregion

        [TestMethod]
        public void Login_Returns_Token()
        {
            var me = _ctx.GetMe();
            Console.WriteLine(me);
        }

        //[TestMethod]
        //public void SetDeviceAccessToken_Returns_Token()
        //{
        //    var token = _ctx.SetDeviceAccessToken("", (a, b, c, d) => { });
        //}

        //[TestMethod]
        //public void SetExtendedAccessToken_Returns_Token()
        //{
        //    _ctx.SetExtendedAccessToken();
        //}
    }
}
