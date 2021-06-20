using Microsoft.VisualStudio.TestTools.UnitTesting;
using NiconicoToolkit.Channels;
using NiconicoToolkit.Community;
using NiconicoToolkit.Live;
using NiconicoToolkit.Mylist;
using NiconicoToolkit.User;
using NiconicoToolkit.Video;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiconicoToolkit.UWP.Test.Tests
{
    [TestClass]
    public sealed class IdTest
    {
        #region Test NiconicoId
        [TestMethod]
        public void NiconicoId_Unknown()
        {
            var id_test = new NiconicoId("123456");
            Assert.AreEqual(NiconicoIdType.Unknown, id_test.IdType);
        }

        [TestMethod]
        public void NiconicoId_VideoIdForUser()
        {
            var id_test = new NiconicoId("sm1234567", NiconicoIdType.Video);
            Assert.IsFalse(id_test.IsUserId);
            Assert.IsTrue(id_test.IsVideoId);
            Assert.IsFalse(id_test.IsVideoAliasId);
            Assert.IsFalse(id_test.IsLiveId);
            Assert.IsFalse(id_test.IsCommunityId);
            Assert.IsFalse(id_test.IsChannelId);
            Assert.IsFalse(id_test.IsMylistId);
        }

        [TestMethod]
        public void NiconicoId_VideoIdForUser_ParsePrefix()
        {
            var videoId_test2 = new NiconicoId("sm1234567");
            Assert.AreEqual(NiconicoIdType.Video, videoId_test2.IdType);
        }

        [TestMethod]
        public void NiconicoId_VideoIdForChannel()
        {
            var id_test = new NiconicoId("so1234567", NiconicoIdType.Video);
            Assert.IsFalse(id_test.IsUserId);
            Assert.IsTrue(id_test.IsVideoId);
            Assert.IsFalse(id_test.IsVideoAliasId);
            Assert.IsFalse(id_test.IsLiveId);
            Assert.IsFalse(id_test.IsCommunityId);
            Assert.IsFalse(id_test.IsChannelId);
            Assert.IsFalse(id_test.IsMylistId);

        }

        [TestMethod]
        public void NiconicoId_VideoIdForChannel_ParsePrefix()
        {
            var videoId_test2 = new NiconicoId("so1234567");
            Assert.AreEqual(NiconicoIdType.Video, videoId_test2.IdType);
        }


        [TestMethod]
        public void NiconicoId_LiveId()
        {
            var id_test = new NiconicoId("lv222222");
            Assert.IsFalse(id_test.IsUserId);
            Assert.IsFalse(id_test.IsVideoId);
            Assert.IsFalse(id_test.IsVideoId);
            Assert.IsFalse(id_test.IsVideoAliasId);
            Assert.IsTrue(id_test.IsLiveId);
            Assert.IsFalse(id_test.IsCommunityId);
            Assert.IsFalse(id_test.IsChannelId);
            Assert.IsFalse(id_test.IsMylistId);
        }


        [TestMethod]
        public void NiconicoId_LiveId_ParsePrefix()
        {
            var videoId_test2 = new NiconicoId("lv1234567");
            Assert.AreEqual(NiconicoIdType.Live, videoId_test2.IdType);
        }

        [TestMethod]
        public void NiconicoId_CommunityId()
        {
            var id_test = new NiconicoId("co1234567", NiconicoIdType.Community);
            Assert.IsFalse(id_test.IsUserId);
            Assert.IsFalse(id_test.IsVideoId);
            Assert.IsFalse(id_test.IsVideoAliasId);
            Assert.IsFalse(id_test.IsLiveId);
            Assert.IsTrue(id_test.IsCommunityId);
            Assert.IsFalse(id_test.IsChannelId);
            Assert.IsFalse(id_test.IsMylistId);
        }

        [TestMethod]
        public void NiconicoId_CommuniyId_ParsePrefix()
        {
            var id_test = new NiconicoId("co1234567");
            Assert.AreEqual(NiconicoIdType.Community, id_test.IdType);
        }

        [TestMethod]
        public void NiconicoId_UserId()
        {
            var id_test = new NiconicoId(1234567, NiconicoIdType.User);
            Assert.IsTrue(id_test.IsUserId);
            Assert.IsFalse(id_test.IsVideoId);
            Assert.IsFalse(id_test.IsVideoAliasId);
            Assert.IsFalse(id_test.IsLiveId);
            Assert.IsFalse(id_test.IsCommunityId);
            Assert.IsFalse(id_test.IsChannelId);
            Assert.IsFalse(id_test.IsMylistId);
        }

        [Ignore]
        [TestMethod]
        public void NiconicoId_Expected_InvalidVideoIdForChannel()
        {
            Assert.ThrowsException<ArgumentException>((Action)(() => 
            {
                var id_test = new NiconicoId("sm1234567", (NiconicoIdType)NiconicoIdType.Video);
            }));
        }

        [Ignore]
        [TestMethod]
        public void NiconicoId_Expected_InvalidId()
        {
            Assert.ThrowsException<ArgumentException>((Action)(() =>
            {
                var id_test = new NiconicoId("sm123456dd", (NiconicoIdType)NiconicoIdType.Video);
            }));
        }

        [Ignore]
        [TestMethod]
        public void NiconicoId_Expected_InvalidUserId()
        {
            Assert.ThrowsException<ArgumentException>(() =>
            {
                var id_test = new NiconicoId("__123456", NiconicoIdType.User);
            });
        }




        [TestMethod]
        public void NiconicoId_Equals()
        {
            var idA = new NiconicoId(123456, NiconicoIdType.User);
            var idB = new NiconicoId("123456");

            Assert.AreEqual(idA, idB);
        }


        [TestMethod]
        public void NiconicoId_NotEquals()
        {
            var idA = new NiconicoId(123456, NiconicoIdType.Video);
            var idB = new NiconicoId("sm1234567", NiconicoIdType.Video);

            Assert.AreNotEqual(idA, idB);
        }

        #endregion Test NiconicoId


        #region Test UserId

        [TestMethod]
        public void UserId_Equeal()
        {
            var idA = new UserId(123456);
            var idB = new UserId("123456");

            Assert.AreEqual(idA, idB);
        }


        [TestMethod]
        public void UserId_ToNiconicoId()
        {
            var idA = new UserId(123456);
            NiconicoId idB = new UserId("123456");

            Assert.AreEqual(NiconicoIdType.User, idB.IdType);
            Assert.AreEqual(idA, idB);
        }

        #endregion



        #region Test VideoId

        [TestMethod]
        public void VideoId()
        {
            var idA = new VideoId("sm12345");
            Assert.AreEqual(VideoIdType.Video, idA.IdType);

            var idB = new VideoId("so012345");
            Assert.AreEqual(VideoIdType.Video, idB.IdType);

            var idC = new VideoId("nm012345");
            Assert.AreEqual(VideoIdType.Video, idC.IdType);
        }


        [TestMethod]
        public void VideoId_ToNiconicoId()
        {
            var idA = new VideoId(123456);
            NiconicoId idB = new VideoId("123456");

            Assert.AreEqual(NiconicoIdType.VideoAlias, idB.IdType);
            Assert.AreEqual(idA, idB);


            VideoId converted = (VideoId)idB;
            Assert.AreEqual(VideoIdType.VideoAlias, converted.IdType);


        }

        #endregion Test VideoId




        #region Test LiveId

        [TestMethod]
        public void LiveId_Equeal()
        {
            var idA = new LiveId(123456);
            var idB = new LiveId("lv123456");

            Assert.AreEqual(idA, idB);
        }


        [TestMethod]
        public void LiveId_ToNiconicoId()
        {
            var idA = new LiveId(123456);
            NiconicoId idB = new LiveId("lv123456");

            Assert.AreEqual(NiconicoIdType.Live, idB.IdType);
            Assert.AreEqual(idA, idB);
        }

        #endregion



        #region Test ChannelId

        [TestMethod]
        public void ChannelId_Equeal()
        {
            var idA = new ChannelId(123456);
            var idB = new ChannelId("ch123456");

            Assert.AreEqual(idA, idB);
        }


        [TestMethod]
        public void ChannelId_ToNiconicoId()
        {
            var idA = new ChannelId(123456);
            NiconicoId idB = new ChannelId("ch123456");

            Assert.AreEqual(NiconicoIdType.Channel, idB.IdType);
            Assert.AreEqual(idA, idB);
        }

        #endregion



        #region Test MylistId

        [TestMethod]
        public void MylistId_Equeal()
        {
            var idA = new MylistId(123456);
            var idB = new MylistId("123456");

            Assert.AreEqual(idA, idB);
        }


        [TestMethod]
        public void MylistId_ToNiconicoId()
        {
            var idA = new MylistId(123456);
            NiconicoId idB = new MylistId("123456");

            Assert.AreEqual(NiconicoIdType.Mylist, idB.IdType);
            Assert.AreEqual(idA, idB);
        }

        #endregion



        #region Test CommunityId

        [TestMethod]
        public void CommunityId_Equeal()
        {
            var idA = new CommunityId(123456);
            var idB = new CommunityId("co123456");

            Assert.AreEqual(idA, idB);
        }


        [TestMethod]
        public void CommunityId_ToNiconicoId()
        {
            var idA = new CommunityId(123456);
            NiconicoId idB = new CommunityId("co123456");

            Assert.AreEqual(NiconicoIdType.Community, idB.IdType);
            Assert.AreEqual(idA, idB);
        }

        #endregion

    }
}

