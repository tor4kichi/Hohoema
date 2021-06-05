using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiconicoToolkit.Video.Watch.NMSG_Comment
{
	public enum ChatResultCode
	{
		Success = 0,
		Failure = 1,
		InvalidThread = 2,
		InvalidTichet = 3,
		InvalidPostkey = 4,
		Locked = 5,
		Readonly = 6,
		Unknown_7 = 7,
		TooLong = 8
	}
}
