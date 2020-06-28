﻿using System.Collections.Generic;

namespace Hohoema.Database
{
    public static class BookmarkDb
    {
        public static List<Bookmark> GetAll()
        {
            var db = HohoemaLiteDb.GetLocalLiteRepository();
            return db.Fetch<Bookmark>();
        }

        public static List<Bookmark> GetAll(BookmarkType bookmarkType)
        {
            var db = HohoemaLiteDb.GetLocalLiteRepository();
            return db.Fetch<Bookmark>(x => x.BookmarkType == bookmarkType);
        }


        public static Bookmark Get(BookmarkType bookmarkType, string content)
        {
            var db = HohoemaLiteDb.GetLocalLiteRepository();
            return db.SingleOrDefault<Bookmark>(x => x.BookmarkType == bookmarkType && x.Content == content);
        }

        public static bool Add(Bookmark bookmark)
        {
            // 重複チェック必要
            var db = HohoemaLiteDb.GetLocalLiteRepository();
            var already = db.SingleOrDefault<Bookmark>(x => x.Content == bookmark.Content && x.BookmarkType == bookmark.BookmarkType);
            if (already != null)
            {
                bookmark.Id = already.Id;
                return false;
            }

            db.Insert(bookmark);

            return true;
        }

        public static bool Remove(Bookmark bookmark)
        {
            var db = HohoemaLiteDb.GetLocalLiteRepository();
            return db.Delete<Bookmark>(bookmark.Id);
        }
    }
}
