using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AutomaEx.Facebook;
using System.IO;

namespace AutomaEx.Tests.Facebook
{
    [TestClass]
    public class UnitTest1
    {
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
            };
        }

        [ClassCleanup]
        public static void ClassCleanup() { if (_ctx != null) _ctx.Dispose(); _ctx = null; }

        [TestMethod]
        public void Can_Login_Returns_Token()
        {
        }
    }
}
