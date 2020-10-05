using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bililive_dm
{
    public class UserGift
    {
        public string User;
        public int UserId;
        public string Gift;
        public int Qty;
        public DateTimeOffset TargetTime;

        public UserGift() => TargetTime = DateTimeOffset.Now + TimeSpan.FromSeconds(StaticConfig.GIFTS_THROTTLE_DURATION);
        public UserGift(string user, int userId, string gift, int qty) : this()
        {
            User = user;
            UserId = userId;
            Gift = gift;
            Qty = qty;
        }

        public bool IsAddable(UserGift gift) => (gift.UserId == UserId) && (gift.Gift == Gift);
    }
}
