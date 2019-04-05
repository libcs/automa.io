using System;
using System.IO;
using System.Net;
using System.Net.Http;

namespace AutomaEx.Facebook
{
    public partial class FacebookContext
    {
        /// <summary>
        /// Gets the facebook URL.
        /// </summary>
        /// <param name="pathAndQuery">The path and query.</param>
        /// <param name="attributes">The attributes.</param>
        /// <param name="postData">The post data.</param>
        /// <returns></returns>
        private string GetFacebookUrl(string pathAndQuery, object attributes = null, string postData = null)
        {
            var url = pathAndQuery.ExpandPathAndQuery(attributes);
            _logger("GetFacebookUrl: " + url);
            return TryFunc(typeof(WebException), () => DownloadData(HttpMethod.Get, url, postData));
        }

        /// <summary>
        /// Downloads the facebook URL.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="pathAndQuery">The path and query.</param>
        /// <param name="attributes">The attributes.</param>
        /// <param name="postData">The post data.</param>
        /// <param name="skipEmptyFile">The skip empty file.</param>
        /// <param name="interceptResponse">The intercept response.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentOutOfRangeException">skipEmptyFile</exception>
        public string DownloadFacebookUrl(string filePath, string pathAndQuery, object attributes = null, string postData = null, FacebookSkipEmptyFile skipEmptyFile = FacebookSkipEmptyFile.None, Action<Stream, Stream> interceptResponse = null)
        {
            var url = pathAndQuery.ExpandPathAndQuery(attributes);
            _logger("DownloadFacebookUrl: " + url);
            var file = TryFunc(typeof(WebException), () => DownloadFile(filePath, HttpMethod.Get, url, postData, interceptResponse: interceptResponse));
            switch (skipEmptyFile)
            {
                case FacebookSkipEmptyFile.None: return file;
                case FacebookSkipEmptyFile.ZeroLength: if (File.ReadAllBytes(file).Length == 0) { File.Delete(file); return null; } return file;
                case FacebookSkipEmptyFile.TextHasSecondLine: if (File.ReadAllLines(file).Length <= 1) { File.Delete(file); return null; } return file;
                default: throw new ArgumentOutOfRangeException("skipEmptyFile");
            }
        }

        // http://stackoverflow.com/questions/9468843/facebook-api-date-format
        /// <summary>
        /// To the date time.
        /// </summary>
        /// <param name="timeStamp">The time stamp.</param>
        /// <returns></returns>
        private static DateTime ToDateTime(double timeStamp)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(timeStamp).ToLocalTime();
        }

        /// <summary>
        /// To the timestamp.
        /// </summary>
        /// <param name="dateTime">The date time.</param>
        /// <returns></returns>
        private static double ToTimestamp(DateTime dateTime)
        {
            return (TimeZoneInfo.ConvertTimeToUtc(dateTime) - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        }
    }
}
