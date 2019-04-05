using AutomaEx.Facebook;
using System;
using System.Linq;
using System.IO;

namespace Sample
{
    class Program_a
    {
        const string SyncPath = "C:\\T_";
        const string AccessTokenFile = "accessToken.json";
        const string CookieFile = "cookies.json";

        static void Main(string[] args)
        {
            var syncPath = SyncPath;
            if (!Directory.Exists(syncPath))
                Directory.CreateDirectory(syncPath);
            var accessTokenFile = Path.Combine(syncPath, AccessTokenFile);
            var cookieFile = Path.Combine(syncPath, CookieFile);
            //
            using (var ctx = new FacebookContext()
            {
                Logger = (x) => Console.WriteLine(x),
                AccessToken = (File.Exists(accessTokenFile) ? File.ReadAllText(accessTokenFile) : null),
                AccessTokenWriter = (x) => File.WriteAllText(accessTokenFile, x),
                CookiesAsString = (File.Exists(cookieFile) ? File.ReadAllText(cookieFile) : null),
                CookiesWriter = (x) => File.WriteAllText(cookieFile, x),
                ServiceCredential = "Facebook",
                AppId = "718092791654479",
                AppSecret = "c2bcfc113b46c1ba81c3ef5a6234945f",
                ClientToken = "c76cf6f38bccfb348e5ed97fb8063c60",
                RequestedScope = "manage_pages,ads_management",
            })
            {
                //var me = ctx.GetMe();

                //var path = @"C:\T_";
                //var pageId = 573293769395845L;
                //var files = ctx.DownloadLeadFormCsvByPage(path, pageId, DateTime.Now.AddDays(-5), null, FacebookSkipEmptyFile.TextHasSecondLine)
                //    .ToArray();

                //var accountId = 789921621154239;
                //ctx.CreateCustomAudience(accountId);
            }
            Console.ReadKey();
        }
    }
}
