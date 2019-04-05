using System;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace AutomaEx
{
    /// <summary>
    /// AutomaExtensions
    /// </summary>
    public static partial class AutomaExtensions
    {
        /// <summary>
        /// Indexes the of skip.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="value">The value.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="ignoreCase">if set to <c>true</c> [ignore case].</param>
        /// <returns>System.Int32.</returns>
        public static int IndexOfSkip(this string source, string value, int startIndex = 0, bool ignoreCase = true)
        {
            var stringComparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return source.IndexOf(value, startIndex, stringComparison) + value.Length;
        }

        /// <summary>
        /// Extracts the span inner.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="start">The start.</param>
        /// <param name="end">The end.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="endIndex">The end index.</param>
        /// <param name="ignoreCase">if set to <c>true</c> [ignore case].</param>
        /// <returns>System.String.</returns>
        public static string ExtractSpanInner(this string source, string start, string end, int startIndex = 0, int endIndex = int.MaxValue, bool ignoreCase = true)
        {
            var stringComparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            var innerIdx1 = source.IndexOf(start, startIndex, stringComparison) + start.Length;
            var innerIdx2 = source.IndexOf(end, innerIdx1);
            return innerIdx1 < endIndex ? source.Substring(innerIdx1, innerIdx2 - innerIdx1) : null;
        }

        /// <summary>
        /// Extracts the span.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="start">The start.</param>
        /// <param name="end">The end.</param>
        /// <param name="ignoreCase">if set to <c>true</c> [ignore case].</param>
        /// <returns>System.String.</returns>
        /// <exception cref="IndexOutOfRangeException">start
        /// or
        /// end</exception>
        public static string ExtractSpan(this string source, string start, string end, bool ignoreCase = true)
        {
            var stringComparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            var sIdx = source.IndexOf(start, stringComparison);
            if (sIdx == -1) throw new IndexOutOfRangeException(nameof(start));
            var eIdx = source.IndexOf(end, sIdx, stringComparison);
            if (eIdx == -1) throw new IndexOutOfRangeException(nameof(end));
            return source.Substring(sIdx, eIdx - sIdx + end.Length);
        }

        /// <summary>
        /// Expands the path and query.
        /// </summary>
        /// <param name="pathAndQuery">The path and query.</param>
        /// <param name="attributes">The attributes.</param>
        /// <returns>System.String.</returns>
        public static string ExpandPathAndQuery(this string pathAndQuery, object attributes)
        {
            if (attributes == null)
                return pathAndQuery;
            var b = new StringBuilder(pathAndQuery);
            object value;
            string valueAsString;
            foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(attributes))
                if ((value = descriptor.GetValue(attributes)) != null && !string.IsNullOrEmpty(valueAsString = value.ToString()))
                    b.Append("&" + descriptor.Name + "=" + valueAsString);
            return b.ToString();
        }

        /// <summary>
        /// Cookies the get set.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="cookies">The cookies.</param>
        /// <returns>CookieCollection.</returns>
        public static CookieCollection CookieGetSet(this AutomaContext source, CookieCollection cookies)
        {
            if (cookies != null)
            {
                source.Cookies = cookies;
                source.CookiesFlush();
            }
            return source.Cookies;
        }

        /// <summary>
        /// Merges the specified cookies.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="cookies">The cookies.</param>
        public static void CookieMerge(this AutomaContext source, CookieCollection cookies)
        {
            var cookiesByName = cookies.Cast<Cookie>().ToDictionary(x => x.Name);
            var newCookies = new CookieCollection { cookies };
            Cookie c;
            foreach (Cookie cookie in source.Cookies)
                if (!cookiesByName.TryGetValue(cookie.Name, out c))
                    newCookies.Add(cookie);
            source.Cookies = newCookies;
        }

        /// <summary>
        /// Timeouts the invoke.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="timeoutMilliseconds">The timeout milliseconds.</param>
        /// <exception cref="TimeoutException"></exception>
        /// <exception cref="System.TimeoutException"></exception>
        public static void TimeoutInvoke(this Action source, int timeoutMilliseconds)
        {
            if (timeoutMilliseconds == 0)
                source();
            Thread threadToKill = null;
            Action action = () => { threadToKill = Thread.CurrentThread; source(); };
            var result = action.BeginInvoke(null, null);
            if (result.AsyncWaitHandle.WaitOne(timeoutMilliseconds))
            {
                action.EndInvoke(result);
                return;
            }
            threadToKill.Abort();
            throw new TimeoutException();
        }

        /// <summary>
        /// Timeouts the invoke.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="timeoutMilliseconds">The timeout milliseconds.</param>
        /// <returns>TResult.</returns>
        /// <exception cref="TimeoutException"></exception>
        /// <exception cref="System.TimeoutException"></exception>
        public static TResult TimeoutInvoke<TResult>(this Func<TResult> source, int timeoutMilliseconds)
        {
            if (timeoutMilliseconds == 0)
                return source();
            Thread threadToKill = null;
            Func<TResult> action = () => { threadToKill = Thread.CurrentThread; return source(); };
            var result = action.BeginInvoke(null, null);
            if (result.AsyncWaitHandle.WaitOne(timeoutMilliseconds))
                return action.EndInvoke(result);
            threadToKill.Abort();
            throw new TimeoutException();
        }
    }
}