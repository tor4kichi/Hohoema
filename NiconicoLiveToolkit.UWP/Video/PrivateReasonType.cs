using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiconicoToolkit.Video
{
	/// <summary>
	/// 動画が非公開にされている理由
	/// </summary>
	public enum PrivateReasonType
	{
		/// <summary>
		/// なし (公開中)
		/// </summary>
		None = 0,

		/// <summary>
		/// 投稿者
		/// </summary>
		PostedBy = 1,

		/// <summary>
		/// 運営
		/// </summary>
		Publishers = 2,

		/// <summary>
		/// 権利者
		/// </summary>
		RightsHolders = 3,

		/// <summary>
		/// 非公開
		/// </summary>
		Private = 8,
	}
}
