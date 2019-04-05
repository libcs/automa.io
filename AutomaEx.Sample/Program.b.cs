using AutomaEx.Facebook;
using AutomaEx.GoogleAdwords;
using System;
using System.IO;

namespace Sample
{
    class Program_b
    {
        const string SyncPath = "C:\\T_";
        const string CookieFile = "cookies.json";

        static void Main_(string[] args)
        {
            var syncPath = SyncPath;
            if (!Directory.Exists(syncPath))
                Directory.CreateDirectory(syncPath);
            var cookieFile = Path.Combine(syncPath, CookieFile);
            //
            using (var ctx = new GoogleAdwordsContext()
            {
                Logger = (x) => Console.WriteLine(x),
                CookiesAsString = (File.Exists(cookieFile) ? File.ReadAllText(cookieFile) : null),
                CookiesWriter = (x) => File.WriteAllText(cookieFile, x),
                ServiceCredential = "Facebook",
                AppId = "303977024514-5d2h3tdp0u84fsqsqtuib0cuhtm8jmt9.apps.googleusercontent.com",
                AppSecret = "UYGvxCy9EJXyWs4ezy31Ef40",
                DeveloperToken = "nTJDXaeDryafy52zThMLlg",
            })
            {
                //ctx.DoAuth2Authorization();
                ctx.AccessToken = "1/1q-YwHeIpPIrl_ym2CIb05MWd0PlvdshZDs4PTq7tbywb5xAaggUlCE0E5JRHEvw";
                ctx.Test("857-327-6907");
            }
            Console.ReadKey();
        }
    }
}
