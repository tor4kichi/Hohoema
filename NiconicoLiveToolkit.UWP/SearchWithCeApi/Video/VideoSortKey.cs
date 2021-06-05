using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiconicoToolkit.SearchWithCeApi.Video
{
	public enum VideoSortKey
	{
		[Description("n")]
		NewComment,    // n
		[Description("v")]
		ViewCount,     // v
		[Description("m")]
		MylistCount,   // m
		[Description("r")]
		CommentCount,  // r
		[Description("f")]
		FirstRetrieve, // f
		[Description("l")]
		Length,        // l
		[Description("h")]
		Popurarity,    // h

		[Description("c")]
		MylistPopurarity, // c
		[Description("n")]
		Relation,      // s
		[Description("i")]
		VideoCount,    // i
	}
}
