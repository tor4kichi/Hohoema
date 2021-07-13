
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NiconicoToolkit.UWP.Test.Tests
{
    [TestClass]
    public class ExtractContenntIdTest
    {
        void CheckExtractIdResult(NiconicoIdType expectedType, NiconicoId? id)
        {
            Assert.IsNotNull(id);
            Assert.AreEqual(expectedType, id.Value.IdType, $"not equal NiconicoIdType expected: {expectedType} actual:{id.Value.IdType}");
        }

        [TestMethod]
        [DataRow("https://www.nicovideo.jp/watch/so31520197?ref=videotop_recommend_tag")]
        [DataRow("https://www.nicovideo.jp/watch/sm38908270?ref=videocate_newarrival")]
        [DataRow("sm38903015")]
        public void ExtractVideoContentId(string urlOrId)
        {
            if (Uri.TryCreate(urlOrId, UriKind.Absolute, out var uri))
            {
                var id = NiconicoUrls.ExtractNicoContentId(uri);
                Assert.IsNotNull(id);
                Assert.IsTrue(id.Value.IdType is NiconicoIdType.Video or NiconicoIdType.Video);
            }
            else
            {
                Assert.IsTrue(NiconicoId.TryCreate(urlOrId, out var id));                
                Assert.IsTrue(id.IdType is NiconicoIdType.Video or NiconicoIdType.Video);                
            }   
        }


        [TestMethod]
        [DataRow("https://live.nicovideo.jp/watch/lv332360112?ref=top_pickup")]
        [DataRow("lv332360112")]
        public void ExtractLiveContentId(string urlOrId)
        {
            if (Uri.TryCreate(urlOrId, UriKind.Absolute, out var uri))
            {
                CheckExtractIdResult(NiconicoIdType.Live, NiconicoUrls.ExtractNicoContentId(uri));
            }
            else
            {
                CheckExtractIdResult(NiconicoIdType.Live, new NiconicoId(urlOrId));
            }
        }

        [TestMethod]
        [DataRow("https://ch.nicovideo.jp/channel/ch2646373")]
        [DataRow("https://ch.nicovideo.jp/higurashianime_202010")]
        [DataRow("https://ch.nicovideo.jp/ch2642363")]
        [DataRow("ch2646373")]
        public void ExtractChannelContentId(string urlOrId)
        {
            if (Uri.TryCreate(urlOrId, UriKind.Absolute, out var uri))
            {
                CheckExtractIdResult(NiconicoIdType.Channel, NiconicoUrls.ExtractNicoContentId(uri));
            }
            else
            {
                CheckExtractIdResult(NiconicoIdType.Channel, new NiconicoId(urlOrId));
            }
        }

        [TestMethod]
        [DataRow("https://com.nicovideo.jp/community/co358573")]
        [DataRow("co358573")]
        public void ExtractCommunityContentId(string urlOrId)
        {
            if (Uri.TryCreate(urlOrId, UriKind.Absolute, out var uri))
            {
                CheckExtractIdResult(NiconicoIdType.Community, NiconicoUrls.ExtractNicoContentId(uri));
            }
            else
            {
                CheckExtractIdResult(NiconicoIdType.Community, new NiconicoId(urlOrId));
            }
        }




        [TestMethod]
        [DataRow("https://www.nicovideo.jp/user/500600/mylist/61896980?ref=pc_userpage_mylist")]
        public void ExtractMylistContentId(string urlOrId)
        {
            if (Uri.TryCreate(urlOrId, UriKind.Absolute, out var uri))
            {
                CheckExtractIdResult(NiconicoIdType.Mylist, NiconicoUrls.ExtractNicoContentId(uri));
            }
            else
            {
                CheckExtractIdResult(NiconicoIdType.Mylist, new NiconicoId(urlOrId));
            }
        }


        
        [TestMethod]
        [DataRow("https://www.nicovideo.jp/series/230847?ref=pc_watch_description_series")]
        public void ExtractSeriesContentId(string urlOrId)
        {
            if (Uri.TryCreate(urlOrId, UriKind.Absolute, out var uri))
            {
                CheckExtractIdResult(NiconicoIdType.Series, NiconicoUrls.ExtractNicoContentId(uri));
            }
            else
            {
                CheckExtractIdResult(NiconicoIdType.Series, new NiconicoId(urlOrId));
            }
        }
    }
}
