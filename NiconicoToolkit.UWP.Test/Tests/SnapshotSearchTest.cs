using Microsoft.Toolkit.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NiconicoToolkit.SnapshotSearch;
using NiconicoToolkit.SnapshotSearch.Filters;
using NiconicoToolkit.SnapshotSearch.JsonFilters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiconicoToolkit.UWP.Test.Tests
{
    [TestClass]
    public sealed class SnapshotSearchTest
    {
        NiconicoContext _context;


        [TestInitialize]
        public async Task Initialize()
        {
            (_context, _, _, _) = await AccountTestHelper.CreateNiconicoContextAndLogInWithTestAccountAsync();
        }



        [TestMethod]
        public async Task AllFieldGettingTest()
        {
            var result = await _context.VideoSnapshotSearch.GetVideoSnapshotSearchAsync(
                "RTA",
                new[] { SearchFieldType.Title, SearchFieldType.Tags, SearchFieldType.Description },
                new SearchSort(SearchFieldType.ViewCounter, SearchSortOrder.Desc),
                "NiconicoToolkit",
                fields: SearchFieldTypeExtensions.FieldTypes.ToArray()
                );

            
            Guard.IsTrue(result.IsSuccess, nameof(result.IsSuccess));

            Guard.IsNotNull(result.Items, nameof(result.Items));

            foreach (var item in result.Items.Take(3))
            {
                Guard.IsNotNull(item.CategoryTags, nameof(item.CategoryTags));
                Guard.IsNotNull(item.Tags, nameof(item.Tags));
                Guard.IsNotNull(item.Title, nameof(item.Title));
                Guard.IsNotNull(item.StartTime, nameof(item.StartTime));
                Guard.IsNotNull(item.ViewCounter, nameof(item.ViewCounter));
                Guard.IsNotNull(item.MylistCounter, nameof(item.MylistCounter));
                Guard.IsNotNull(item.CommentCounter, nameof(item.CommentCounter));
                Guard.IsNotNull(item.LikeCounter, nameof(item.LikeCounter));
                Guard.IsNotNull(item.ThumbnailUrl, nameof(item.ThumbnailUrl));
                Guard.IsNotNull(item.ContentId, nameof(item.ContentId));
                Guard.IsNotNull(item.Genre, nameof(item.Genre));
                Guard.IsNotNull(item.LengthSeconds, nameof(item.LengthSeconds));
                Guard.IsNotNull(item.LastCommentTime, nameof(item.LastCommentTime));
                Guard.IsNotNull(item.Description, nameof(item.Description));
            }
        }


        [TestMethod]
        public async Task ContainsFilterTest()
        {
            var result = await _context.VideoSnapshotSearch.GetVideoSnapshotSearchAsync(
                "",
                new[] { SearchFieldType.Title, SearchFieldType.Tags, SearchFieldType.Description },
                new SearchSort(SearchFieldType.ViewCounter, SearchSortOrder.Desc),
                "NiconicoToolkit",
                fields: SearchFieldTypeExtensions.FieldTypes.ToArray(),
                filter: new ValueContainsSearchFilter<string>(SearchFieldType.Genre, "ゲーム", "アニメ")
                );


            Guard.IsTrue(result.IsSuccess, nameof(result.IsSuccess));

            Guard.IsNotNull(result.Items, nameof(result.Items));

            foreach (var item in result.Items.Take(3))
            {
                Guard.IsNotNull(item.CategoryTags, nameof(item.CategoryTags));
                Guard.IsNotNull(item.Tags, nameof(item.Tags));
                Guard.IsNotNull(item.Title, nameof(item.Title));
                Guard.IsNotNull(item.StartTime, nameof(item.StartTime));
                Guard.IsNotNull(item.ViewCounter, nameof(item.ViewCounter));
                Guard.IsNotNull(item.MylistCounter, nameof(item.MylistCounter));
                Guard.IsNotNull(item.CommentCounter, nameof(item.CommentCounter));
                Guard.IsNotNull(item.LikeCounter, nameof(item.LikeCounter));
                Guard.IsNotNull(item.ThumbnailUrl, nameof(item.ThumbnailUrl));
                Guard.IsNotNull(item.ContentId, nameof(item.ContentId));
                Guard.IsNotNull(item.Genre, nameof(item.Genre));
                Guard.IsNotNull(item.LengthSeconds, nameof(item.LengthSeconds));
                Guard.IsNotNull(item.LastCommentTime, nameof(item.LastCommentTime));
                Guard.IsNotNull(item.Description, nameof(item.Description));
            }
        }

        [TestMethod]
        public async Task CompareFilterTest()
        {
            var result = await _context.VideoSnapshotSearch.GetVideoSnapshotSearchAsync(
                "RTA",
                new[] { SearchFieldType.Title, SearchFieldType.Tags, SearchFieldType.Description },
                new SearchSort(SearchFieldType.ViewCounter, SearchSortOrder.Desc),
                "NiconicoToolkit",
                fields: SearchFieldTypeExtensions.FieldTypes.ToArray(),
                filter: new CompareSearchFilter<string>(SearchFieldType.Genre, "ゲーム", SearchFilterCompareCondition.Equal)
                );


            Guard.IsTrue(result.IsSuccess, nameof(result.IsSuccess));

            Guard.IsNotNull(result.Items, nameof(result.Items));

            foreach (var item in result.Items.Take(3))
            {
                Guard.IsNotNull(item.CategoryTags, nameof(item.CategoryTags));
                Guard.IsNotNull(item.Tags, nameof(item.Tags));
                Guard.IsNotNull(item.Title, nameof(item.Title));
                Guard.IsNotNull(item.StartTime, nameof(item.StartTime));
                Guard.IsNotNull(item.ViewCounter, nameof(item.ViewCounter));
                Guard.IsNotNull(item.MylistCounter, nameof(item.MylistCounter));
                Guard.IsNotNull(item.CommentCounter, nameof(item.CommentCounter));
                Guard.IsNotNull(item.LikeCounter, nameof(item.LikeCounter));
                Guard.IsNotNull(item.ThumbnailUrl, nameof(item.ThumbnailUrl));
                Guard.IsNotNull(item.ContentId, nameof(item.ContentId));
                Guard.IsNotNull(item.Genre, nameof(item.Genre));
                Guard.IsNotNull(item.LengthSeconds, nameof(item.LengthSeconds));
                Guard.IsNotNull(item.LastCommentTime, nameof(item.LastCommentTime));
                Guard.IsNotNull(item.Description, nameof(item.Description));
            }
        }


        [TestMethod]
        public async Task JsonFilterTest()
        {
            var result = await _context.VideoSnapshotSearch.GetVideoSnapshotSearchAsync(
                "RTA",
                new[] { SearchFieldType.Title, SearchFieldType.Tags, SearchFieldType.Description },
                new SearchSort(SearchFieldType.ViewCounter, SearchSortOrder.Desc),
                "NiconicoToolkit",
                fields: SearchFieldTypeExtensions.FieldTypes.ToArray(),
                filter: new AndJsonFilter(new IJsonSearchFilter[] { new EqaulJsonFilter<string>(SearchFieldType.Genre, "アニメ"), new RangeJsonFilter<TimeSpan>(SearchFieldType.LengthSeconds, 1200, null) })
                );


            Guard.IsTrue(result.IsSuccess, nameof(result.IsSuccess));

            Guard.IsNotNull(result.Items, nameof(result.Items));

            foreach (var item in result.Items.Take(3))
            {
                Guard.IsNotNull(item.CategoryTags, nameof(item.CategoryTags));
                Guard.IsNotNull(item.Tags, nameof(item.Tags));
                Guard.IsNotNull(item.Title, nameof(item.Title));
                Guard.IsNotNull(item.StartTime, nameof(item.StartTime));
                Guard.IsNotNull(item.ViewCounter, nameof(item.ViewCounter));
                Guard.IsNotNull(item.MylistCounter, nameof(item.MylistCounter));
                Guard.IsNotNull(item.CommentCounter, nameof(item.CommentCounter));
                Guard.IsNotNull(item.LikeCounter, nameof(item.LikeCounter));
                Guard.IsNotNull(item.ThumbnailUrl, nameof(item.ThumbnailUrl));
                Guard.IsNotNull(item.ContentId, nameof(item.ContentId));
                Guard.IsNotNull(item.Genre, nameof(item.Genre));
                Guard.IsNotNull(item.LengthSeconds, nameof(item.LengthSeconds));
                Guard.IsNotNull(item.LastCommentTime, nameof(item.LastCommentTime));
                Guard.IsNotNull(item.Description, nameof(item.Description));
            }
        }
    }
}
