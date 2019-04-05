using System.Net;
using System.Net.Http;

namespace AutomaEx.Facebook
{
    public partial class FacebookContext
    {
        /// <summary>
        /// Creates the custom audience.
        /// </summary>
        /// <param name="accountId">The account identifier.</param>
        /// <returns></returns>
        public string CreateCustomAudience(long accountId)
        {
            EnsureAppIdAndSecret();
            return TryFunc<string>(() =>
            {
                var r = DownloadJson(HttpMethod.Post, $"{BASEv}/act_{accountId}/customaudiences".ExpandPathAndQuery(new { subtype = "CUSTOM" }));
                return null;
            });
        }

        //private IEnumerable<JsonObject> GetLeadFormsByPage(long pageId)
        //{
        //    var cursor = (Func<FacebookContext, JsonObject>)(x => x.F.Get<JsonObject>(pageId.ToString() + "/leadgen_forms"));
        //    while (cursor != null)
        //    {
        //        var r = TryFunc(cursor);
        //        var paging = (JsonObject)r["paging"];
        //        cursor = (paging.ContainsKey("next") ? (Func<FacebookContext, JsonObject>)(x => x.F.Get<JsonObject>((string)paging["next"])) : null);
        //        foreach (JsonObject i in (JsonArray)r["data"])
        //            yield return i;
        //    }
        //}
    }
}
