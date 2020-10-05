using BiliDMLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Bililive_dm
{
    public class LoginDataUpdatedEventArgs : EventArgs
    {
        /// <summary>
        /// 登录数据出现更新的用户
        /// </summary>
        public User User { get; }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <param name="user"></param>
        public LoginDataUpdatedEventArgs(User user)
        {
            User = user ?? throw new ArgumentNullException(nameof(user));
        }
    }
    public struct ExpiresInResult { 
        public bool flag;
        public int expiresIn;

        public ExpiresInResult(bool f, int e) {
            flag = f;
            expiresIn = e;
        }
    }
    /// <summary>
    /// 登录API扩展类，提供快速操作
    /// </summary>
    public static class LoginApiExtensions
    {
        /// <summary>
        /// 在用户登录数据更新时发生
        /// </summary>
        public static event EventHandler<LoginDataUpdatedEventArgs> LoginDataUpdated;

        /// <summary>
        /// 登录
        /// </summary>
        /// <param name="user">用户</param>
        /// <returns></returns>
        public static async Task<bool> Login(this User user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            ExpiresInResult eir;
            string key; 
            string json;
            LoginResult result;

            if (user.HasData)
            {
                // 如果以前登录过，判断一下需不需要重新登录
                // 这个API每次登录有效时间是720小时（expires_in=2592000）
                eir = await user.GetExpiresIn();
                if (eir.flag)
                {
                    // Token有效
                    if (eir.expiresIn < 1800)
                    {
                        // Token有效，但是有效时间太短，小于半个小时
                        user.LogWarning("Token有效时间不足，将刷新Token。");
                        return user.IsLogin = await user.RefreshToken();
                    }
                    else
                    {
                        // Token有效时间足够
                        user.LogInfo("使用缓存Token登录成功。");
                        user.LogInfo($"Token有效时间还剩：{Math.Round(eir.expiresIn / 3600d, 1)} 小时。");
                        return user.IsLogin = true;
                    }
                }
            }
            // 不存在登录数据，这是第一次登录
            try
            {
                key = await LoginApi.GetKeyAsync(user);
                json = await LoginApi.LoginAsync(user, key, null);
                if (string.IsNullOrEmpty(json))
                {
                    throw new Exception("登录失败，没有返回的数据。");
                }
                result = JsonHelper.DeserializeJsonToObject<LoginResult>(json);
            }
            catch (Exception ex)
            {
                user.LogError("登录失败");
                throw ex;
            }
            if (result.Code == 0 && result.Data.Status == 0)
            {
                // 登录成功，保存数据直接返回
                UpdateLoginData(user, result);
                OnLoginDataUpdated(new LoginDataUpdatedEventArgs(user));
                user.LogInfo("登录成功，用户id="+ user.Data.Uid);
                return user.IsLogin = true;
            }
            else if (result.Code == -105)
            {
                // 需要验证码
                return await LoginWithCaptcha(user, key);
            }
            else
            {
                // 其它错误
                user.LogError("登录失败");
                user.LogError($"错误信息：{Utils.FormatJson(json)}");
                return false;
            }
        }

        /// <summary>
        /// 尝试获取Token过期时间
        /// </summary>
        /// <param name="user">用户</param>
        /// <returns></returns>
        public static async Task<ExpiresInResult> GetExpiresIn(this User user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            LoginResult result;

            try
            {
                string json = await LoginApi.GetInfoAsync(user);
                if (string.IsNullOrEmpty(json))
                {
                    throw new Exception("获取Token过期时间失败，没有返回的数据。");
                }
                result = JsonHelper.DeserializeJsonToObject<LoginResult>(json);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            if (result.Code == 0 && !string.IsNullOrEmpty(result.Data.Mid))
            {
                return new ExpiresInResult(true, result.Data.ExpiresIn);
            }
            else
            {
                return new ExpiresInResult(false, 0);
            }
        }

        /// <summary>
        /// 刷新Token
        /// </summary>
        /// <param name="user">用户</param>
        /// <returns></returns>
        public static async Task<bool> RefreshToken(this User user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            string json;
            RefreshTokenResult result;

            try
            {
                json = await LoginApi.RefreshTokenAsync(user);
                if (string.IsNullOrEmpty(json))
                {
                    throw new Exception("登录失败，没有返回的数据。");
                }
                if (json.ToLower().Contains("doctype html"))
                {
                    throw new Exception("登录失败，刷新Token失败。");
                }

                result = JsonHelper.DeserializeJsonToObject<RefreshTokenResult>(json);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            if (result.Code == 0 && result.Data != null && !string.IsNullOrEmpty(result.Data.Mid))
            {
                user.LogInfo("Token刷新成功");
                UpdateLoginData(user, result.Data);
                OnLoginDataUpdated(new LoginDataUpdatedEventArgs(user));
                return true;
            }
            else
            {
                user.LogError("Token刷新失败");
                user.LogError($"错误信息：{Utils.FormatJson(json)}");
                return false;
            }
        }

        private static async Task<bool> LoginWithCaptcha(User user, string key)
        {
            string json;
            LoginResult result;

            try
            {
                string captcha;

                captcha = await LoginApi.SolveCaptchaAsync(await LoginApi.GetCaptchaAsync(user));
                json = await LoginApi.LoginAsync(user, key, captcha);
                if (string.IsNullOrEmpty(json))
                {
                    throw new Exception("登录失败，没有返回的数据。");
                }
                result = JsonHelper.DeserializeJsonToObject<LoginResult>(json);
            }
            catch (Exception ex)
            {
                user.LogError("登录失败");
                throw ex;
            }
            if (result.Code == 0 && result.Data.Status == 0)
            {
                // 登录成功，保存数据直接返回
                user.LogInfo("登录成功");
                UpdateLoginData(user, result);
                OnLoginDataUpdated(new LoginDataUpdatedEventArgs(user));
                return true;
            }
            else
            {
                // 其它错误
                user.LogError("登录失败");
                user.LogError($"错误信息：{Utils.FormatJson(json)}");
                return false;
            }
        }

        private static void UpdateLoginData(User user, RefreshTokenResultData data)
        {
            try
            {
                if (user.Data == null)
                {
                    user.Data = new LoginData();
                }

                user.Data.AccessKey = data.AccessToken;
                user.Data.RefreshToken = data.RefreshToken;
            }
            catch (Exception ex)
            {
                throw new Exception($"Update user login data failed. {ex.Message}");
            }
        }

        private static void UpdateLoginData(User user, LoginResult result)
        {
            try
            {
                if (user.Data == null)
                {
                    user.Data = new LoginData();
                }

                LoginResultData data = result.Data;

                user.Data.AccessKey = data.TokenInfo.AccessToken;
                user.Data.Cookie = string.Join(";", data.CookieInfo.Cookies.Select(t => t.Name + "=" + t.Value));
                CookiesItem cookie = data.CookieInfo.Cookies.SingleOrDefault(t => t.Name == "bili_jct");
                if (cookie == null)
                {
                    throw new Exception("Can not get csrf by cookie.");
                }
                user.Data.Csrf = cookie.Value;
                user.Data.RefreshToken = data.TokenInfo.RefreshToken;
                cookie = data.CookieInfo.Cookies.SingleOrDefault(t => t.Name == "DedeUserID");
                if (cookie == null)
                {
                    throw new Exception("Can not get uid by cookie.");
                }
                user.Data.Uid = data.CookieInfo.Cookies.SingleOrDefault(t => t.Name == "DedeUserID").Value;
            }
            catch (Exception ex)
            {
                throw new Exception($"Update user login data failed. {ex.Message}");
            }
        }

        private static void OnLoginDataUpdated(LoginDataUpdatedEventArgs e)
        {
            LoginDataUpdated?.Invoke(null, e);
        }
    }
}
