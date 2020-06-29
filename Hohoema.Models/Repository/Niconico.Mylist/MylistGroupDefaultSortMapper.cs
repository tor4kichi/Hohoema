using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Repository.Niconico.Mylist
{
    internal static class MylistGroupDefaultSortMapper
    {
        public static MylistGroupDefaultSort ToModelDefaultSort(this Mntone.Nico2.Mylist.MylistDefaultSort sort) => sort switch
        {
            Mntone.Nico2.Mylist.MylistDefaultSort.Old => MylistGroupDefaultSort.Old,
            Mntone.Nico2.Mylist.MylistDefaultSort.Latest => MylistGroupDefaultSort.Latest,
            Mntone.Nico2.Mylist.MylistDefaultSort.Memo_Ascending => MylistGroupDefaultSort.Memo_Ascending,
            Mntone.Nico2.Mylist.MylistDefaultSort.Memo_Descending => MylistGroupDefaultSort.Memo_Descending,
            Mntone.Nico2.Mylist.MylistDefaultSort.Title_Ascending => MylistGroupDefaultSort.Title_Ascending,
            Mntone.Nico2.Mylist.MylistDefaultSort.Title_Descending => MylistGroupDefaultSort.Title_Descending,
            Mntone.Nico2.Mylist.MylistDefaultSort.FirstRetrieve_Ascending => MylistGroupDefaultSort.FirstRetrieve_Ascending,
            Mntone.Nico2.Mylist.MylistDefaultSort.FirstRetrieve_Descending => MylistGroupDefaultSort.FirstRetrieve_Descending,
            Mntone.Nico2.Mylist.MylistDefaultSort.View_Ascending => MylistGroupDefaultSort.View_Ascending,
            Mntone.Nico2.Mylist.MylistDefaultSort.View_Descending => MylistGroupDefaultSort.View_Descending,
            Mntone.Nico2.Mylist.MylistDefaultSort.Comment_New => MylistGroupDefaultSort.Comment_New,
            Mntone.Nico2.Mylist.MylistDefaultSort.Comment_Old => MylistGroupDefaultSort.Comment_Old,
            Mntone.Nico2.Mylist.MylistDefaultSort.CommentCount_Ascending => MylistGroupDefaultSort.CommentCount_Ascending,
            Mntone.Nico2.Mylist.MylistDefaultSort.CommentCount_Descending => MylistGroupDefaultSort.CommentCount_Descending,
            Mntone.Nico2.Mylist.MylistDefaultSort.MylistCount_Ascending => MylistGroupDefaultSort.MylistCount_Ascending,
            Mntone.Nico2.Mylist.MylistDefaultSort.MylistCount_Descending => MylistGroupDefaultSort.MylistCount_Descending,
            Mntone.Nico2.Mylist.MylistDefaultSort.Length_Ascending => MylistGroupDefaultSort.Length_Ascending,
            Mntone.Nico2.Mylist.MylistDefaultSort.Length_Descending => MylistGroupDefaultSort.Length_Descending,
            _ => throw new NotSupportedException(sort.ToString()),
        };

        public static Mntone.Nico2.Mylist.MylistDefaultSort ToInfrastructureDefaultSort(this MylistGroupDefaultSort sort) => sort switch
        {
            MylistGroupDefaultSort.Old => Mntone.Nico2.Mylist.MylistDefaultSort.Old,
            MylistGroupDefaultSort.Latest => Mntone.Nico2.Mylist.MylistDefaultSort.Latest,
            MylistGroupDefaultSort.Memo_Ascending => Mntone.Nico2.Mylist.MylistDefaultSort.Memo_Ascending,
            MylistGroupDefaultSort.Memo_Descending => Mntone.Nico2.Mylist.MylistDefaultSort.Memo_Descending,
            MylistGroupDefaultSort.Title_Ascending => Mntone.Nico2.Mylist.MylistDefaultSort.Title_Ascending,
            MylistGroupDefaultSort.Title_Descending => Mntone.Nico2.Mylist.MylistDefaultSort.Title_Descending,
            MylistGroupDefaultSort.FirstRetrieve_Ascending => Mntone.Nico2.Mylist.MylistDefaultSort.FirstRetrieve_Ascending,
            MylistGroupDefaultSort.FirstRetrieve_Descending => Mntone.Nico2.Mylist.MylistDefaultSort.FirstRetrieve_Descending,
            MylistGroupDefaultSort.View_Ascending => Mntone.Nico2.Mylist.MylistDefaultSort.View_Ascending,
            MylistGroupDefaultSort.View_Descending => Mntone.Nico2.Mylist.MylistDefaultSort.View_Descending,
            MylistGroupDefaultSort.Comment_New => Mntone.Nico2.Mylist.MylistDefaultSort.Comment_New,
            MylistGroupDefaultSort.Comment_Old => Mntone.Nico2.Mylist.MylistDefaultSort.Comment_Old,
            MylistGroupDefaultSort.CommentCount_Ascending => Mntone.Nico2.Mylist.MylistDefaultSort.CommentCount_Ascending,
            MylistGroupDefaultSort.CommentCount_Descending => Mntone.Nico2.Mylist.MylistDefaultSort.CommentCount_Descending,
            MylistGroupDefaultSort.MylistCount_Ascending => Mntone.Nico2.Mylist.MylistDefaultSort.MylistCount_Ascending,
            MylistGroupDefaultSort.MylistCount_Descending => Mntone.Nico2.Mylist.MylistDefaultSort.MylistCount_Descending,
            MylistGroupDefaultSort.Length_Ascending => Mntone.Nico2.Mylist.MylistDefaultSort.Length_Ascending,
            MylistGroupDefaultSort.Length_Descending => Mntone.Nico2.Mylist.MylistDefaultSort.Length_Descending,
            _ => throw new NotSupportedException(sort.ToString()),
        };

    }
}
