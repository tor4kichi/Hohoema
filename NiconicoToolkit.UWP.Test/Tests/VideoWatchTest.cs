using Microsoft.VisualStudio.TestTools.UnitTesting;
using NiconicoToolkit.Video.Watch.Dmc;
using NiconicoToolkit.Video.Watch.NMSG_Comment;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Streaming.Adaptive;

namespace NiconicoToolkit.UWP.Test.Tests
{
    [TestClass]
    public sealed class VideoWatchTest
    {
        [TestInitialize]
        public void Initialize()
        {
            _context = new NiconicoContext("HohoemaTest");
        }

        NiconicoContext _context;



        #region Watch Video

        [TestMethod]
        [DataRow("sm38647727")]
        public async Task PlayVideoProgressiveMp4Async(string videoId)
        {
            var res = await _context.Video.VideoWatch.GetInitialWatchDataAsync(videoId, false, false);
            Assert.IsNotNull(res.WatchApiResponse.WatchApiData.Media.Delivery);

            var movie = res.WatchApiResponse.WatchApiData.Media.Delivery.Movie;
            var session = await _context.Video.VideoWatch.GetDmcSessionResponseAsync(
                res.WatchApiResponse.WatchApiData, movie.Videos.FirstOrDefault(x => x.IsAvailable), movie.Audios.FirstOrDefault(x => x.IsAvailable)
                );

            await OpenProgressiveMp4Async(session);
        }

        [TestMethod]
        [DataRow("sm38647727")]
        public async Task PlayVideoForceHlsAsync(string videoId)
        {
            var res = await _context.Video.VideoWatch.GetInitialWatchDataAsync(videoId, false, false);
            Assert.IsNotNull(res.WatchApiResponse.WatchApiData.Media.Delivery);

            var movie = res.WatchApiResponse.WatchApiData.Media.Delivery.Movie;
            var session = await _context.Video.VideoWatch.GetDmcSessionResponseAsync(
                res.WatchApiResponse.WatchApiData, movie.Videos.FirstOrDefault(x => x.IsAvailable), movie.Audios.FirstOrDefault(x => x.IsAvailable)
                , hlsMode: true
                );

            await OpenHlsAsync(session);
        }

        [TestMethod]
        [DataRow("so38538458")]
        public async Task PlayVideoHlsAsync(string videoId)
        {
            var res = await _context.Video.VideoWatch.GetInitialWatchDataAsync(videoId, false, false);
            Assert.IsNotNull(res.WatchApiResponse.WatchApiData.Media.Delivery);
            var movie = res.WatchApiResponse.WatchApiData.Media.Delivery.Movie;
            var session = await _context.Video.VideoWatch.GetDmcSessionResponseAsync(
                res.WatchApiResponse.WatchApiData, movie.Videos.FirstOrDefault(x => x.IsAvailable), movie.Audios.FirstOrDefault(x => x.IsAvailable)
                , hlsMode: true
                );

            await OpenHlsAsync(session);
        }

        [TestMethod]
        [DataRow("so38538458")]
        public async Task PlayVideoForceProgressiveMp4Async(string videoId)
        {
            var res = await _context.Video.VideoWatch.GetInitialWatchDataAsync(videoId, false, false);
            Assert.IsNotNull(res.WatchApiResponse.WatchApiData.Media.Delivery);
            var movie = res.WatchApiResponse.WatchApiData.Media.Delivery.Movie;
            var session = await _context.Video.VideoWatch.GetDmcSessionResponseAsync(
                res.WatchApiResponse.WatchApiData, movie.Videos.FirstOrDefault(x => x.IsAvailable), movie.Audios.FirstOrDefault(x => x.IsAvailable)
                , hlsMode: false
                );

            await OpenProgressiveMp4Async(session);
        }

        private async Task OpenProgressiveMp4Async(DmcSessionResponse session)
        {
            Assert.IsTrue(HttpStatusCodeHelper.IsSuccessStatusCode(session.Meta.Status));
            Debug.WriteLineIf(session.Meta.Message is not null, session.Meta.Message);

            Assert.IsNotNull(session.Data.Session.ContentUri);
            // Try open media
            using (var mediaSource = MediaSource.CreateFromUri(session.Data.Session.ContentUri))
            {
                await mediaSource.OpenAsync();
            }
        }

        private async Task OpenHlsAsync(DmcSessionResponse session)
        {
            Assert.IsTrue(HttpStatusCodeHelper.IsSuccessStatusCode(session.Meta.Status));
            Debug.WriteLineIf(session.Meta.Message is not null, session.Meta.Message);
            Assert.IsNotNull(session.Data.Session.ContentUri);
            Assert.AreEqual("mpeg2ts", session.Data.Session.Protocol.Parameters.HttpParameters.Parameters.HlsParameters.MediaSegmentFormat);

            // Try open media
            var ams = await AdaptiveMediaSource.CreateFromUriAsync(session.Data.Session.ContentUri, _context.HttpClient);
            Assert.AreEqual(ams.Status, AdaptiveMediaSourceCreationStatus.Success);

            using (var mediaSource = MediaSource.CreateFromAdaptiveMediaSource(ams.MediaSource))
            {
                await mediaSource.OpenAsync();
            }
        }


        [TestMethod]
        [DataRow("so38750114")]
        public async Task GetAdmissionRequireWatchAsync(string videoId)
        {
            var res = await _context.Video.VideoWatch.GetInitialWatchDataAsync(videoId, false, false);

            Assert.IsNull(res.WatchApiResponse.WatchApiData.Media.Delivery);
        }

        #endregion



        #region Comment


        [TestMethod]
        [DataRow("sm38647727")]
        [DataRow("so38538458")]
        public async Task GetCommentAsync(string videoId)
        {
            var res = await _context.Video.VideoWatch.GetInitialWatchDataAsync(videoId, false, false);

            Assert.IsNotNull(res.WatchApiResponse.WatchApiData.Comment);

            var commentSession = new CommentSession(_context, res.WatchApiResponse.WatchApiData);

            var commentRes = await commentSession.GetCommentFirstAsync();

            Assert.IsNotNull(commentRes.Comments);
            Assert.IsNotNull(commentRes.Leaves);
            Assert.IsNotNull(commentRes.Threads);

            if (commentRes.GlobalNumRes.NumRes > 0)
            {
                Assert.IsTrue(commentRes.Comments.Any());
            }
        }

        #endregion Comment
    }
}
