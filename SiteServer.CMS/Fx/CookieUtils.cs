using System;
using System.Web;
using SiteServer.Utils;

namespace SiteServer.CMS.Fx
{
    public static class CookieUtils
    {
        public static void SetCookie(string name, string value, TimeSpan expiresAt, bool isEncrypt = true)
        {
            SetCookie(new HttpCookie(name)
            {
                Value = value,
                Expires = DateUtils.GetExpiresAt(expiresAt),
                Domain = FxUtils.HttpContextRootDomain
            }, isEncrypt);
        }

        public static void SetCookie(string name, string value, bool isEncrypt = true)
        {
            SetCookie(new HttpCookie(name)
            {
                Value = value,
                Domain = FxUtils.HttpContextRootDomain
            }, isEncrypt);
        }

        private static void SetCookie(HttpCookie cookie, bool isEncrypt)
        {
            cookie.Value = isEncrypt ? TranslateUtils.EncryptStringBySecretKey(cookie.Value) : cookie.Value;
            cookie.HttpOnly = false;

            if (HttpContext.Current.Request.Url.Scheme.Equals("https"))
            {
                cookie.Secure = true;//通过https传递cookie
            }
            HttpContext.Current.Response.Cookies.Add(cookie);
        }

        public static string GetCookie(string name, bool isDecrypt = true)
        {
            if (HttpContext.Current.Request.Cookies[name] == null) return string.Empty;

            var value = HttpContext.Current.Request.Cookies[name].Value;
            return isDecrypt ? TranslateUtils.DecryptStringBySecretKey(value) : value;
        }

        public static bool IsExists(string name)
        {
            return HttpContext.Current.Request.Cookies[name] != null;
        }

        public static void Erase(string name)
        {
            if (HttpContext.Current.Request.Cookies[name] != null)
            {
                SetCookie(name, string.Empty, TimeSpan.FromDays(-1));
            }
        }
    }
}
