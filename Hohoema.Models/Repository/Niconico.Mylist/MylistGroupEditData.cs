using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Repository.Niconico.Mylist
{
	public class MylistGroupEditData
	{
		public MylistGroupEditData()
		{

		}

		public string Name { get; set; }
		public string Description { get; set; } = string.Empty;
		public MylistGroupIconType IconType { get; set; }
		public bool IsPublic { get; set; }
		public MylistGroupDefaultSort MylistDefaultSort { get; set; }
	}
}
