﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hohoema.Interfaces;

namespace Hohoema.Database
{

    public class Feed 
    {
        /// <summary>
        /// データベース向けのID（自動採番、変更不可）
        /// </summary>
        [LiteDB.BsonId(autoId:true)]
        public int Id { get; set; }

        /// <summary>
        /// 表示名
        /// </summary>
        [LiteDB.BsonField]
        public string Label { get; set; }


        /// <summary>
        /// Itemsを最後に更新した日時
        /// </summary>
        [LiteDB.BsonField]
        public DateTime UpdateAt { get; set; }

        /// <summary>
        /// ユーザーが新着を確認をした日時
        /// </summary>
        [LiteDB.BsonField]
        public DateTime CheckedAt { get; set; } = DateTime.MinValue;

        /// <summary>
        /// 動画リスト情報の取得元リスト。
        /// </summary>
        [LiteDB.BsonRef]
        public List<Bookmark> Sources { get; set; } = new List<Bookmark>();

        #region Interface INiconicoContent

        [LiteDB.BsonIgnore]
        string _Id;

        #endregion



        public bool AddSource(Bookmark bookmark)
        {
            if (!Sources.Any(x => x.Label == bookmark.Label && x.BookmarkType == bookmark.BookmarkType))
            {
                BookmarkDb.Add(bookmark);

                Sources.Add(bookmark);

                Database.FeedDb.AddOrUpdate(this);

                return true;
            }
            else
            {
                return false;
            }
        }


        public Feed() { }

        public Feed(string label)
        {
            Label = label;
        }

    }
}
