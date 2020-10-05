using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.ComponentModel;
using Accessibility;

namespace Bililive_dm
{
    /// <summary>
    /// 表示一个Bilibili用户
    /// </summary>
    public sealed class User : IDisposable
    {
        private readonly string _account;
        private readonly string _password;
        private readonly MainWindow _win;
        private readonly HttpClient _handler;
        private readonly Dictionary<string, string> _pcHeaders;
        private readonly Dictionary<string, string> _appHeaders;
        private bool _isDisposed;
        public bool IsLogin { get; set; } = false;
        public string VisitId { get; set; }

        public string Account => _account;
        public string Password => _password;
        public MainWindow Win => _win;
        public HttpClient Handler => _handler;

        public Dictionary<string, string> PCHeaders => _pcHeaders;
        public Dictionary<string, string> AppHeaders => _appHeaders;
        public LoginData Data { get; set; }

        /// <summary>
        /// 是否存在登录数据（不保证数据有效，只判断是否存在数据）
        /// </summary>
        public bool HasData => (Data != null && !string.IsNullOrEmpty(Data.Uid));

        /// <summary>
        /// 构造器（用于反序列化，虽然Json.NET可以使用有参构造器，但是如果程序被混淆，反序列化将失败）
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public User()
        {
            _handler = new HttpClient(new HttpClientHandler { UseCookies = false })
            {
                Timeout = TimeSpan.FromMilliseconds(3000)
            };
            _pcHeaders = new Dictionary<string, string>();
            _appHeaders = new Dictionary<string, string>();
            Data = null;
            Initialize();
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <param name="account">账号</param>
        /// <param name="password">密码</param>
        public User(string account, string password) : this()
        {
            if (account == null)
                throw new ArgumentNullException(nameof(account));
            if (password == null)
                throw new ArgumentNullException(nameof(password));

            _account = account;
            _password = password;
        }

        /// <summary>
        /// 构造器
        /// </summary>
        /// <param name="account">账号</param>
        /// <param name="password">密码</param>
        public User(string account, string password, MainWindow win) : this()
        {
            if (account == null)
                throw new ArgumentNullException(nameof(account));
            if (password == null)
                throw new ArgumentNullException(nameof(password));

            _account = account;
            _password = password;
            _win = win;
        }

        /// <summary>
        /// 初始化
        /// </summary>
        public void Initialize()
        {
            _pcHeaders.Clear();
            _pcHeaders["Accept"] = "application/json, text/plain, */*";
            _pcHeaders["Accept-Encoding"] = "deflate";
            _pcHeaders["Accept-Language"] = "zh-CN,zh;q:0.9";
            _pcHeaders["User-Agent"] = "Mozilla/5.0 BiliDroid/5.57.2 (ccc@gmail.com) os/android model/MI 9 mobi_app/android build/5572000 channel/master innerVer/5572000 osVer/10 network/2";
            _appHeaders.Clear();
            _appHeaders["Buvid3"] = "000ce0b9b9b4e342ad4f421bcae5e0ce";
            _appHeaders["Accept-Encoding"] = "deflate";
            _appHeaders["Display-ID"] = "146771405-1521008435";
            _appHeaders["User-Agent"] = "bili-universal/6570 CFNetwork/894 Darwin/17.4.0";
        }

        /// <summary>
        /// 将登录数据导入到 <see cref="PCHeaders"/> 和 <see cref="AppHeaders"/>
        /// </summary>
        public void ImportLoginData()
        {
            if (!HasData)
                return;
            UpdateDictionary(_pcHeaders, Data);
            UpdateDictionary(_appHeaders, Data);
        }

        /// <summary>
        /// 清除所有缓存数据以及登录数据
        /// </summary>
        public void Clear()
        {
            Initialize();
            IsLogin = false;
            Data = null;
        }

        /// <summary />
        public override string ToString()
        {
            return _account;
        }

        /// <summary />
        public void Dispose()
        {
            if (_isDisposed)
                return;
            _handler.Dispose();
            _isDisposed = true;
        }

        private static void UpdateDictionary(Dictionary<string, string> target, Dictionary<string, string> source)
        {
            foreach (KeyValuePair<string, string> item in source)
                target[item.Key] = item.Value;
        }
        private static void UpdateDictionary(Dictionary<string, string> target, LoginData source)
        {
            if (target.ContainsKey("access_key"))
            {
                target["access_key"] = source.AccessKey;
            }
            if (target.ContainsKey("cookie"))
            {
                target["cookie"] = source.Cookie;
            }
            if (target.ContainsKey("csrf"))
            {
                target["csrf"] = source.Csrf;
            }
            if (target.ContainsKey("refresh_token"))
            {
                target["refresh_token"] = source.RefreshToken;
            }
            if (target.ContainsKey("uid"))
            {
                target["uid"] = source.Uid;
            }
        }
    }
}
