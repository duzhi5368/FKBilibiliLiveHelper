using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bililive_dm
{
    public class CookiesItem
    {
        /// <summary>
        /// 
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("http_only")]
        public int HttpOnly { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int Expires { get; set; }
    }

    public class CookieInfo
    {
        /// <summary>
        /// 
        /// </summary>
        public List<CookiesItem> Cookies { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public List<string> Domains { get; set; }
    }

    public class TokenInfo
    {
        /// <summary>
        /// 
        /// </summary>
        public string Mid { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }
    }
    public class LoginResultData
    {
        /// <summary>
        /// 
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Mid { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("token_info")]
        public TokenInfo TokenInfo { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("cookie_info")]
        public CookieInfo CookieInfo { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public List<string> SSO { get; set; }
    }

    public class SendMessageResult
    {
        /// <summary>
        /// 
        /// </summary>
        public string msg { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int code { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string message { get; set; }
    }

    public class LoginResult
    {
        /// <summary>
        /// 
        /// </summary>
        public int Ts { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int Code { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public LoginResultData Data { get; set; }
    }

    class RefreshTokenResult
    {
        /// <summary>
        /// 
        /// </summary>
        public int Ts { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int Code { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public RefreshTokenResultData Data { get; set; }
    }

    class RefreshTokenResultData
    {
        /// <summary>
        /// 
        /// </summary>
        public string Mid { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
