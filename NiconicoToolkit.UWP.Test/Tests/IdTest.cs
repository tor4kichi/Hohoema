using Microsoft.VisualStudio.TestTools.UnitTesting;
using NiconicoToolkit.Live;
using NiconicoToolkit.User;
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
            Assert.AreEqual(NiconicoContentIdType.Unknown, id_test.ContentIdType);
        }

        [TestMethod]
        public void NiconicoId_VideoIdForUser()
        {
            var id_test = new NiconicoId("sm1234567", NiconicoContentIdType.VideoForUser);
            Assert.IsFalse(id_test.IsUserId);
            Assert.IsTrue(id_test.IsVideoId);
            Assert.IsTrue(id_test.IsVideoIdForUser);
            Assert.IsFalse(id_test.IsVideoIdForChannel);
            Assert.IsFalse(id_test.IsLiveId);
            Assert.IsFalse(id_test.IsCommunityId);
            Assert.IsFalse(id_test.IsChannelId);
            Assert.IsFalse(id_test.IsMylistId);
        }

        [TestMethod]
        public void NiconicoId_VideoIdForUser_ParsePrefix()
        {
            var videoId_test2 = new NiconicoId("sm1234567");
            Assert.AreEqual(NiconicoContentIdType.VideoForUser, videoId_test2.ContentIdType);
        }

        [TestMethod]
        public void NiconicoId_VideoIdForChannel()
        {
            var id_test = new NiconicoId("so1234567", NiconicoContentIdType.VideoForChannel);
            Assert.IsFalse(id_test.IsUserId);
            Assert.IsTrue(id_test.IsVideoId);
            Assert.IsFalse(id_test.IsVideoIdForUser);
            Assert.IsTrue(id_test.IsVideoIdForChannel);
            Assert.IsFalse(id_test.IsLiveId);
            Assert.IsFalse(id_test.IsCommunityId);
            Assert.IsFalse(id_test.IsChannelId);
            Assert.IsFalse(id_test.IsMylistId);

        }

        [TestMethod]
        public void NiconicoId_VideoIdForChannel_ParsePrefix()
        {
            var videoId_test2 = new NiconicoId("so1234567");
            Assert.AreEqual(NiconicoContentIdType.VideoForChannel, videoId_test2.ContentIdType);
        }


        [TestMethod]
        public void NiconicoId_LiveId()
        {
            var id_test = new NiconicoId("lv222222");
            Assert.IsFalse(id_test.IsUserId);
            Assert.IsFalse(id_test.IsVideoId);
            Assert.IsFalse(id_test.IsVideoIdForUser);
            Assert.IsFalse(id_test.IsVideoIdForChannel);
            Assert.IsTrue(id_test.IsLiveId);
            Assert.IsFalse(id_test.IsCommunityId);
            Assert.IsFalse(id_test.IsChannelId);
            Assert.IsFalse(id_test.IsMylistId);
        }


        [TestMethod]
        public void NiconicoId_LiveId_ParsePrefix()
        {
            var videoId_test2 = new NiconicoId("lv1234567");
            Assert.AreEqual(NiconicoContentIdType.Live, videoId_test2.ContentIdType);
        }

        [TestMethod]
        public void NiconicoId_CommunityId()
        {
            var id_test = new NiconicoId("co1234567", NiconicoContentIdType.Community);
            Assert.IsFalse(id_test.IsUserId);
            Assert.IsFalse(id_test.IsVideoId);
            Assert.IsFalse(id_test.IsVideoIdForUser);
            Assert.IsFalse(id_test.IsVideoIdForChannel);
            Assert.IsFalse(id_test.IsLiveId);
            Assert.IsTrue(id_test.IsCommunityId);
            Assert.IsFalse(id_test.IsChannelId);
            Assert.IsFalse(id_test.IsMylistId);
        }

        [TestMethod]
        public void NiconicoId_CommuniyId_ParsePrefix()
        {
            var id_test = new NiconicoId("co1234567");
            Assert.AreEqual(NiconicoContentIdType.Community, id_test.ContentIdType);
        }

        [TestMethod]
        public void NiconicoId_UserId()
        {
            var id_test = new NiconicoId(1234567, NiconicoContentIdType.User);
            Assert.IsTrue(id_test.IsUserId);
            Assert.IsFalse(id_test.IsVideoId);
            Assert.IsFalse(id_test.IsVideoIdForUser);
            Assert.IsFalse(id_test.IsVideoIdForChannel);
            Assert.IsFalse(id_test.IsLiveId);
            Assert.IsFalse(id_test.IsCommunityId);
            Assert.IsFalse(id_test.IsChannelId);
            Assert.IsFalse(id_test.IsMylistId);
        }

        [TestMethod]
        public void NiconicoId_Expected_InvalidVideoIdForChannel()
        {
            Assert.ThrowsException<ArgumentException>(() => 
            {
                var id_test = new NiconicoId("sm1234567", NiconicoContentIdType.VideoForChannel);
            });
        }

        [TestMethod]
        public void NiconicoId_Expected_InvalidId()
        {
            Assert.ThrowsException<ArgumentException>(() =>
            {
                var id_test = new NiconicoId("sm123456dd", NiconicoContentIdType.VideoForChannel);
            });
        }

        [TestMethod]
        public void NiconicoId_Expected_InvalidUserId()
        {
            Assert.ThrowsException<ArgumentException>(() =>
            {
                var id_test = new NiconicoId("__123456", NiconicoContentIdType.User);
            });
        }




        [TestMethod]
        public void NiconicoId_Equals()
        {
            var idA = new NiconicoId(123456, NiconicoContentIdType.User);
            var idB = new NiconicoId("123456");

            Assert.AreEqual(idA, idB);
        }


        [TestMethod]
        public void NiconicoId_NotEquals()
        {
            var idA = new NiconicoId(123456, NiconicoContentIdType.VideoForUser);
            var idB = new NiconicoId("sm1234567", NiconicoContentIdType.VideoForUser);

            Assert.AreNotEqual(idA, idB);
        }

        #endregion Test NiconicoId


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

            Assert.AreEqual(NiconicoContentIdType.Live, idB.ContentIdType);
            Assert.AreEqual(idA, idB);
        }

        #endregion

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

            Assert.AreEqual(NiconicoContentIdType.User, idB.ContentIdType);
            Assert.AreEqual(idA, idB);
        }

        #endregion


    }
}

