using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;

namespace AutomaEx.Facebook
{
    public partial class FacebookContext
    {
        /// <summary>
        /// Downloads the lead form CSV by page.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="pageId">The page identifier.</param>
        /// <param name="fromDate">From date.</param>
        /// <param name="toDate">To date.</param>
        /// <param name="skipEmptyFile">The skip empty file.</param>
        /// <returns></returns>
        public IEnumerable<string> DownloadLeadFormCsvByPage(string path, long pageId, DateTime? fromDate, DateTime? toDate, FacebookSkipEmptyFile skipEmptyFile = FacebookSkipEmptyFile.None)
        {
            _logger("DownloadLeadFormCsvByPage: " + pageId);
            EnsureAppIdAndSecret();
            EnsureRequestedScope();
            var maps = LoadCsvPageMaps(path, pageId);
            var query = GetLeadFormsByPage(pageId)
                .Select(x => new { id = x["id"], name = (string)x["name"], url = ToBusinessUrl((string)x["leadgen_export_csv_url"]) });
            CsvPageMap[] map;
            foreach (var item in query)
            {
                _logger(item.id.ToString() + " - " + item.name);
                var file = DownloadFacebookUrl(path, item.url, new
                {
                    from_date = (fromDate != null ? (int?)ToTimestamp(fromDate.Value) : null),
                    to_date = (toDate != null ? (int?)ToTimestamp(toDate.Value) : null),
                }, null, skipEmptyFile, (maps.TryGetValue(item.id.ToString(), out map) ? (Action<Stream, Stream>)((a, b) => CsvPageIntercept(item.id.ToString(), map, a, b)) : null));
                if (file != null)
                    yield return file;
            }
        }

        private static string ToBusinessUrl(string url)
        {
            return (url != null ? url.Replace("www.facebook.com", "business.facebook.com") : null);
        }

        private class CsvPageMap
        {
            public string f { get; set; }
            public string t { get; set; }
        }

        private IDictionary<string, CsvPageMap[]> LoadCsvPageMaps(string path, long pageId)
        {
            return Directory.GetFiles(path, "*.map")
                .Select(x => new { Name = Path.GetFileNameWithoutExtension(x), Map = JsonConvert.DeserializeObject<CsvPageMap[]>(File.ReadAllText(x)) })
                .ToDictionary(x => x.Name, x => x.Map);
        }

        private void CsvPageIntercept(string mapName, CsvPageMap[] map, Stream fileStream, Stream input)
        {
            _logger(string.Format("CsvPageIntercept: {0}[{1}]", mapName, map.Length));
            var buf = new MemoryStream();
            var v = input.ReadByte();
            while (v != -1)
            {
                buf.WriteByte((byte)v);
                if (v == '\n')
                    break;
                v = input.ReadByte();
            }
            buf.Position = 0;
            var line = buf.ToArray();
            if (v == '\n')
                foreach (var m in map)
                {
                    var search = Encoding.Unicode.GetBytes(m.f);
                    var repl = Encoding.Unicode.GetBytes(m.t);
                    int fidx = 0;
                    var index = Array.FindIndex(line, 0, line.Length, (byte b) =>
                    {
                        fidx = (b == search[fidx] ? fidx + 1 : 0);
                        return (fidx == search.Length);
                    }) - search.Length + 1;
                    if (index > -1)
                    {
                        var newLine = new byte[line.Length - search.Length + repl.Length];
                        Buffer.BlockCopy(line, 0, newLine, 0, index);
                        Buffer.BlockCopy(repl, 0, newLine, index, repl.Length);
                        Buffer.BlockCopy(line, index + search.Length, newLine, index + repl.Length, line.Length - (index + search.Length));
                        line = newLine;
                    }
                }
            fileStream.Write(line, 0, line.Length);
        }

        private IEnumerable<JToken> GetLeadFormsByPage(long pageId)
        {
            LoadMeAccounts();
            var cursor = (Func<JToken>)(() => DownloadJson(HttpMethod.Get, $"{BASEv}/{pageId}/leadgen_forms", interceptRequest: r => InterceptRequestForAccount(r, pageId)));
            while (cursor != null)
            {
                var r = TryFunc(typeof(WebException), cursor);
                var paging = (IDictionary<string, JToken>)r["paging"];
                cursor = (paging.ContainsKey("next") ? (Func<JToken>)(() => DownloadJson(HttpMethod.Get, (string)paging["next"], interceptRequest: r2 => InterceptRequestForAccount(r2, pageId))) : null);
                foreach (JObject i in (JArray)r["data"])
                    yield return i;
            }
        }
    }
}
