using Mntone.Nico2;
using Mntone.Nico2.Live.PlayerStatus;
using Mntone.Nico2.Videos.Comment;
using NicoPlayerHohoema.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace NicoPlayerHohoema.Models.Live
{
	// nico live comment -> see@ http://blog.ingen084.net/blog/1691



	public delegate void NicoLiveCommentPostedEventHandler(bool isSuccess);
	public delegate void NicoLiveCommentRecievedEventHandler(Chat chat);
	public delegate void NicoLiveCommentServerConnectedEventHandler();

	public delegate void NicoLiveHeartbeatEventHandler(uint commentCount, uint watchCount);

	public delegate void NicoLiveEndConnectedEventHandler();


	public sealed class NicoLiveCommentClient : IDisposable
	{
		public NiconicoContext NiconicoContext { get; private set; }

		private byte[] _Buffer = new byte[1024];

		TcpClient _Client;

		public string LiveId { get; private set; }
		public string ThreadId { get; private set; }
		public uint ThreadIdNumber { get; private set; }

		public string Host { get; private set; }
		public ushort Port { get; private set; }

		CancellationTokenSource _LiveCommentServerRecieveCancelSource;
		Task _LiveCommentServerRecieveTask;

		AsyncLock _NetworkStreamLock = new AsyncLock();
		NetworkStream _NetworkStream;
		public event NicoLiveCommentServerConnectedEventHandler CommentServerConnected;
		public event NicoLiveCommentRecievedEventHandler CommentRecieved;
		public event NicoLiveCommentPostedEventHandler CommentPosted;

		public event NicoLiveHeartbeatEventHandler Heartbeat;

		public event NicoLiveEndConnectedEventHandler EndConnect;

		private DateTimeOffset _BaseTime;
		private long _ServerTime;
		private string _Ticket;
		public bool IsCommentServerConnected { get; private set; }


		AsyncLock _HeartbeatTimerLock = new AsyncLock();
		Timer _HeartbeatTimer;
		TimeSpan _HeartbeatInterval = TimeSpan.FromSeconds(90);

		public uint CommentCount { get; private set; }
		public uint WatchCount { get; private set; }

		public NicoLiveCommentClient(string liveId, DateTimeOffset baseTime, CommentServer commentServer, NiconicoContext context)
		{
			LiveId = liveId;
			NiconicoContext = context;
			ThreadIdNumber = commentServer.ThreadIds.ElementAt(0);
			Host = commentServer.Host;
			Port = commentServer.Port;

			_BaseTime = baseTime;

			_Client = new TcpClient();
		}

		public async Task Start()
		{
			await Stop();

			await _Client.ConnectAsync(Host, Port);

			_LiveCommentServerRecieveCancelSource = new CancellationTokenSource();
			using (var releaser = await _NetworkStreamLock.LockAsync())
			{
				_NetworkStream = _Client.GetStream();

				_LiveCommentServerRecieveTask = DataRecivingAction();

				var s = $"<thread thread=\"{ThreadIdNumber}\" version=\"20061206\" res_from=\"-0\" />\0";
				var writer = new StreamWriter(_NetworkStream, Encoding.UTF8);
				await writer.WriteAsync(s);
				writer.Flush();
			}




			// ハートビートを開始
			await StartHeartbeatTimer();
		}

		

		public async Task Stop()
		{
			using (var releaser = await _NetworkStreamLock.LockAsync())
			{
				_LiveCommentServerRecieveCancelSource?.Cancel();

				await Task.Delay(1000);

				_LiveCommentServerRecieveCancelSource?.Dispose();
				_LiveCommentServerRecieveCancelSource = null;

				_NetworkStream?.Dispose();
				_NetworkStream = null;
			}

			_LiveCommentServerRecieveTask = null;

			await ExitHeartbeatTimer();
		}


		public async Task PostComment(string comment, uint userId, string command)
		{
			var postkey = await NiconicoContext.Live.GetPostKeyAsync(ThreadIdNumber, CommentCount / 100);
			if (string.IsNullOrEmpty(postkey))
			{
				return;
			}

			PostChat chat = new PostChat();
			chat.Comment = Uri.EscapeUriString(comment);
			chat.ThreadId = ThreadId;
			chat.PostKey = postkey;
			chat.Ticket = _Ticket;
			chat.Mail = command;
			chat.Premium = "";
			chat.UserId = userId.ToString();
			chat.Vpos = ((_ServerTime - _BaseTime.Ticks) * 100).ToString();
			try
			{
				var chatSerializer = new XmlSerializer(typeof(PostChat));

				using (var releaser = await _NetworkStreamLock.LockAsync())
				{
					var writer = _NetworkStream.AsOutputStream().AsStreamForWrite();
					chatSerializer.Serialize(writer, chat);
					writer.WriteByte(0);
					writer.Flush();
				}
			}
			catch { }
		}



		public void Dispose()
		{
			Stop()
				.ContinueWith((prevTask) => _Client.Dispose())
				.ConfigureAwait(false);
		}






		private async Task DataRecivingAction()
		{
			if (_NetworkStream == null ) { return; }

			Debug.WriteLine("start comment recieve.");

			try
			{
				var recieveByteCount = 0;

				while ((recieveByteCount = await _NetworkStream.ReadAsync(_Buffer, 0, _Buffer.Length, _LiveCommentServerRecieveCancelSource.Token)) != 0)
				{
					var s = Encoding.UTF8.GetString(_Buffer);

					Debug.WriteLine($"コメントサーバーから受信:{_Buffer.Length}B");

					var nullStrStart = s.IndexOf('\0');
					ParseLiveCommentServerResponse(s.Remove(nullStrStart));

					Array.Clear(_Buffer, 0, _Buffer.Length);
					await Task.Delay(50);
				}
			}
			catch { }

			Debug.WriteLine("exit comment recieve.");
		}


		private void ParseLiveCommentServerResponse(string recievedString)
		{
			if (string.IsNullOrWhiteSpace(recievedString))
			{
				Debug.WriteLine($"受信した文字列が無効");
				return;
			}

			Debug.WriteLine(recievedString);

			if (!recievedString.StartsWith("<") || !recievedString.EndsWith(">"))
			{
				Debug.WriteLine($"受信した文字列はXml形式ではない");
				return;
			}

			var xmlDoc = XDocument.Parse(recievedString);
			var xmlRoot = xmlDoc.Root;
			var elementName = xmlRoot.Name.LocalName;
			if (elementName == "thread")
			{
				Debug.WriteLine("threadとして解析開始");
				IsCommentServerConnected = true;

				// <thread ticket="{チケット}" server_time="{サーバー時刻}" last_res="{送信される過去のコメント数？(NECOで使用してないので不明)}">
				var serverTimeText = xmlRoot.Attribute(XName.Get("server_time")).Value;
				long serverTime;
				if (long.TryParse(serverTimeText, out serverTime))
				{
					_ServerTime = serverTime;
				}

				var ticketText = xmlRoot.Attribute(XName.Get("ticket")).Value;
				_Ticket = ticketText;

				CommentServerConnected?.Invoke();
			}
			else if (elementName == "chat_result")
			{
				Debug.WriteLine("chat_resultとして解析開始");
				// <chat_result status="{コメント投稿要求の返答}" />
				var result = xmlRoot.Attribute(XName.Get("status")).Value;

				/*
				0 = 投稿に成功した
				1 = 投稿に失敗した(短時間に同じ内容のコメントを投稿しようとした、パラメータが間違っている、他)
				4 = 投稿に失敗した(ごく短時間にコメントを連投しようとした、パラメータが間違っている、他)
					*/
				CommentPosted?.Invoke(result == "0");
			}
			else if (elementName == "chat")
			{
				Debug.WriteLine("chatとして解析開始");

				// _LiveComments.Add(chat);
				// <chat anonymity="{184か}" no="{コメントの番号}" date="{コメントが投稿されたリアル時間？}" mail="{コマンド}" premium="{プレミアムID}"　thread="{スレッドID}" user_id="{ユーザーID}" vpos="{コメントが投稿された生放送の時間}" score="{NGスコア}">{コメント}</chat>\0
				try
				{
					var chatSerializer = new XmlSerializer(typeof(Chat));
					using (var readerStream = new StringReader(recievedString))
					{
						var chat = chatSerializer.Deserialize(readerStream) as Chat;
						if (chat != null)
						{
							CommentRecieved?.Invoke(chat);
						}
					}
				}
				catch { }
			}
			else
			{
				Debug.WriteLine($"受信した文字列はサポートされてません。");
			}

		}





		/// <summary>
		/// ニコ生へのコメント送信の有効性を保つために
		/// LiveAPIのハートビートへ定期的にアクセスする<br />
		/// </summary>
		/// <returns></returns>
		/// <remarks>https://www59.atwiki.jp/nicoapi/pages/19.html</remarks>
		private async Task TryHeartbeat()
		{
			using (var releaser = await _HeartbeatTimerLock.LockAsync())
			{
				if (LiveId == null) { return; }

				try
				{
					var res = await NiconicoContext.Live.HeartbeatAsync(LiveId);

					Debug.WriteLine("heartbeat to " + LiveId);

					await HohoemaApp.UIDispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
					{
						// TODO: 視聴者数やコメント数の更新
						CommentCount = res.CommentCount;
						WatchCount = res.WatchCount;
						Heartbeat?.Invoke(res.CommentCount, res.WatchCount);
					});
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex.ToString());
					// ハートビートに失敗した場合は、放送終了か追い出された
					EndConnect?.Invoke();
				}
			}
		}


		private async Task StartHeartbeatTimer()
		{
			await ExitHeartbeatTimer();

			using (var releaser = await _HeartbeatTimerLock.LockAsync())
			{
				_HeartbeatTimer = new Timer(
					async state => await TryHeartbeat(),
					null,
					TimeSpan.FromSeconds(5), // いきなりハートビートを叩くとダメっぽいので最初は遅らせる
					_HeartbeatInterval
					);
			}

			Debug.WriteLine("start heartbeat to " + LiveId);
		}


		private async Task ExitHeartbeatTimer()
		{
			using (var releaser = await _HeartbeatTimerLock.LockAsync())
			{
				if (_HeartbeatTimer != null)
				{
					_HeartbeatTimer.Dispose();
					_HeartbeatTimer = null;

					Debug.WriteLine("exit heartbeat to " + LiveId);
				}
			}
		}

	}

	[XmlRoot(ElementName = "chat")]
	public class PostChat
	{
		[XmlText]
		public string Comment { get; set; }

		[XmlAttribute(AttributeName = "thread")]
		public string ThreadId { get; set; }
		[XmlAttribute(AttributeName = "ticket")]
		public string Ticket { get; set; }
		[XmlAttribute(AttributeName = "vpos")]
		public string Vpos { get; set; }
		[XmlAttribute(AttributeName = "postkey")]
		public string PostKey { get; set; }
		[XmlAttribute(AttributeName = "mail")]
		public string Mail { get; set; }
		[XmlAttribute(AttributeName = "user_id")]
		public string UserId { get; set; }
		[XmlAttribute(AttributeName = "premium")]
		public string Premium { get; set; }
	}
}
