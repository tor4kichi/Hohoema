using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Niconico.Account
{
	public enum NiconicoSignInStatus
	{
		/// <summary>
		/// 二段階認証によるコード認証が必要
		/// </summary>
		TwoFactorAuthRequired = -3,

		/// <summary>
		/// サービス停止中
		/// </summary>
		/// <remarks>
		/// niconico がメインテナンス中の可能性があります
		/// </remarks>
		ServiceUnavailable = -2,

		/// <summary>
		/// ログインに失敗した
		/// </summary>
		Failed = -1,

		/// <summary>
		/// ログインに成功した
		/// </summary>
		Success = 1,
	}
}
