using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bililive_dm
{
	public static class UserExtensions
	{
		/// <summary />
		public static void LogNewLine(this User user)
		{
			if (user == null)
				throw new ArgumentNullException(nameof(user));

			if (user.Win != null) {
				user.Win.Logging("\r\n");
			}
		}

		/// <summary />
		public static void LogInfo(this User user, string value)
		{
			if (user == null)
				throw new ArgumentNullException(nameof(user));

			if (user.Win != null)
			{
				user.Win.Logging($"用户\"{user}\"：{value}");
			}
		}

		/// <summary />
		public static void LogWarning(this User user, string value)
		{
			if (user == null)
				throw new ArgumentNullException(nameof(user));

			if (user.Win != null)
			{
				user.Win.Logging($"用户\"{user}\"：{value}");
			}
		}

		/// <summary />
		public static void LogError(this User user, string value)
		{
			if (user == null)
				throw new ArgumentNullException(nameof(user));

			if (user.Win != null)
			{
				user.Win.ErrorLogging($"用户\"{user}\"：{value}");
			}
		}
	}
}
