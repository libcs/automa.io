using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AutomaEx.GoogleAdwords;
using System.IO;

namespace AutomaEx.Tests.GoogleAdwords
{
    [TestClass]
    public class UnitTest1
    {
        const string SyncPath = "C:\\T_";
        const string CookieFile = "cookies.json";
        static GoogleAdwordsContext _ctx = null;

        [ClassInitialize]
        public static void ClassInitialize(TestContext ctx)
        {
            var syncPath = SyncPath;
            if (!Directory.Exists(syncPath))
                Directory.CreateDirectory(syncPath);
            var cookieFile = Path.Combine(syncPath, CookieFile);
            _ctx = new GoogleAdwordsContext
            {
                Logger = (x) => Console.WriteLine(x),
                CookiesAsString = (File.Exists(cookieFile) ? File.ReadAllText(cookieFile) : null),
                CookiesWriter = (x) => File.WriteAllText(cookieFile, x),
                ServiceCredential = "GoogleAdwords",
            };
        }

        [ClassCleanup]
        public static void ClassCleanup() { if (_ctx != null) _ctx.Dispose(); _ctx = null; }

        [TestMethod]
        public void TestMethod1()
        {
        }
    }
}
