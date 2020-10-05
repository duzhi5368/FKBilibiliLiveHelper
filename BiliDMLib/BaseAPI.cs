using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiliDMLib
{
    public static class BaseAPI
    {
        public static string GetUserNameByUserId(int userId)
        {
            IDictionary<string, string> headers = new Dictionary<string, string>
            {
                { "Origin", "https://space.bilibili.com" },
                { "Referer", $"https://space.bilibili.com/{userId}/" },
                { "X-Requested-With", "XMLHttpRequest" }
            };
            string json = HttpHelper.HttpPost("https://space.bilibili.com/ajax/member/GetInfo", $"mid={userId}&csrf=", headers: headers);
            JObject j = JObject.Parse(json);
            if (j["status"].ToObject<bool>())
            {
                return j["data"]["name"].ToString();
            }
            else
            {
                throw new NotImplementedException($"未知的服务器返回:{j.ToString(0)}");
            }
        }

        public static IDictionary<string, string> Titles { get; } = new Dictionary<string, string>
        {
            { "title-4-1", "超·年糕团长" },
            { "title-9-1", "真·圣诞爸爸" },
            { "title-10-1", "圣·尼古拉斯" },
            { "title-39-1", "7th.Annv" },
            { "title-46-1", "甘すぎる" },
            { "title-47-1", "King" },
            { "title-58-1", "夜空花火" },
            { "title-62-1", "[小电视]应援" },
            { "title-63-1", "[22]应援" },
            { "title-64-1", "[33]应援" },
            { "title-65-1", "(黄)STAR" },
            { "title-66-1", "(紫)STAR" },
            { "title-67-1", "(蓝)STAR" },
            { "title-68-1", "(青)STAR" },
            { "title-69-1", "(红)STAR" },
            { "title-70-1", "(黄)SUPERSTAR" },
            { "title-71-1", "(紫)SUPERSTAR" },
            { "title-72-1", "(蓝)SUPERSTAR" },
            { "title-73-1", "(青)SUPERSTAR" },
            { "title-74-1", "(红)SUPERSTAR" },
            { "title-77-1", "Miss 椛" },
            { "title-80-1", "SANTA☆CLAUS" },
            { "title-92-1", "年兽" },
            { "title-93-1", "注孤生" },
            { "title-99-1", "神域阐释者" },
            { "title-107-1", "神州" },
            { "title-111-1", "bilibili link" },
            { "title-113-1", "[小电视]应援(复刻)" },
            { "title-114-1", "[22]应援(复刻)" },
            { "title-115-1", "[33]应援(复刻)" },
            { "title-119-1", "五魅首" },
            { "title-128-2", "唯望若安" },
            { "title-139-1", "雷狮海盗" },
            { "title-147-1", "LPL2018" },
            { "title-156-1", "一本满足(复刻)" },
            { "title-157-1", "吃瓜群众(复刻)" },
            { "title-164-1", "PK" },
            { "title-165-1", "最佳助攻" },
            { "title-166-1", "Cantus Knight" },
            { "title-167-1", "Rhythm Saber" },
            { "title-179-1", "时光守护" },
            { "title-190-1", "锦鲤" },
            { "title-201-1", "震惊" },
            { "title-224-1", "Kizuner(初级)" },
            { "title-225-1", "Kizuner(高级)" },
            { "title-241-1", "BilibiliWorld" },
            { "title-243-1", "治愈笑颜" },
            { "title-245-1", "mikufans/245" },
            { "title-263-1", "打电话" },
            { "title-270-1", "FFF团员(复刻)" },
            { "title-281-1", "2019-IVC" },
            { "title-284-1", "音·轨迹" },
            { "title-285-1", "mikufans/285" },
            { "title-290-1", "蘑菇♥1219" },
            { "title-293-1", "LPL2020" },
            { "title-296-1", "奥利给" },
            { "title-301-1", "OWL2020" },
            { "title-308-1", "这把算我赢" },
            { "title-310-1", "2020KPL" },
            { "title-315-1", "镜出动计划" },
            { "title-319-1", "COA-3" },
            { "title-324-1", "蹦迪玩家" },
            { "title-326-1", "星玩家" },
        };

        public static void UpdateTitles()
        {
            string json = HttpHelper.HttpGet("https://api.live.bilibili.com/rc/v1/Title/webTitles", 5);
            JObject j = JObject.Parse(json);
            int code = j["code"].ToObject<int>();
            if (code == 0)
            {
                foreach (JToken jt in j["data"].Where(p => !Titles.ContainsKey(p["identification"].ToString())))
                {
                    Titles.Add(jt["identification"].ToString(), jt["name"].ToString().Trim());
                }
            }
        }
    }
}