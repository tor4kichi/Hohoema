using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NiconicoToolkit.Comment
{
	public enum CommandType
	{
		small,
		big,
		medium,

		ue,
		shita,
		naka,

		white,              // #FFFFFF
		red,                // #FF0000
		pink,               // #FF8080
		orange,             // #FFC000
		yellow,             // #FFFF00
		green,              // #00FF00
		cyan,               // #00FFFF
		blue,               // #0000FF
		purple,             // #C000FF
		black,              // #000000


		white2,             // #CCCC99
		niconicowhite,      // #CCCC99
		red2,               // #CC0033
		truered,            // #CC0033
		pink2,              // #FF33CC
		orange2,            // #FF6600
		passionorange,      // #FF6600
		yellow2,            // #999900
		madyellow,          // #999900
		green2,             // #00CC66
		elementalgreen,     // #00CC66
		cyan2,              // #00CCCC
		blue2,              // #3399FF (ニコ生では#33FFFC)
		marineblue,         // #3399FF
		purple2,            // #6633CC
		nobleviolet,        // #6633CC
		black2,             // #666666

		full,

		_184,
		invisible,          // 流れるコメントには非表示、コメント一覧にのみ表示
		all,                // ng設定を全て有効化

		from_button,        // 投稿者ボタンから投稿されたコメント
		is_button,          // 投稿者ボタンのコメント
		_live               // 新着

	}


	public static class CommandTypesHelper
	{
		public static List<CommandType> ParseCommentCommandTypes(string mail)
		{
			if (mail == null)
			{
				return new List<CommandType>();
			}

			return mail.Split(' ').Select(x =>
			{
				CommandType temp;
				if (Enum.TryParse<CommandType>(x, out temp))
				{
					return new Nullable<CommandType>(temp);
				}
				else
				{
					return new Nullable<CommandType>();
				}
			})
			.Where(x => x.HasValue)
			.Select(x => x.Value)
			.ToList();
		}
	}
}
