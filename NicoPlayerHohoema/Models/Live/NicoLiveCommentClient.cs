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
	// Note: NetworkStreamはスレッドセーフではないようので、こちらで非同期ロックを噛ませた操作をしています

	// Note: NetworkStream.ReadAsyncに渡したキャンセルトークンは
	// キャンセルをしてもReadAsync内部ではOperationCanceledExceptionをトリガーしません。
	// このため、NetworkStream.DataAvailableをチェックしながらデータ到着を待機した上で
	// 到着したタイミングに限定してNetworkStream.ReadAsyncを実行することで
	// ネットワークデータ読み取りのキャンセルを実現しています

	// Note: ハートビートはコメント送信権の活性化のために行っています
	// ハートビートを送っていない場合は３分でコメント送信権がなくなるようです
	// ハートビートのレスポンスのWaitDurationに合わせて90秒ごとにハートビートを送っています

	// nico live comment -> see@ http://blog.ingen084.net/blog/1691


	public delegate void NicoLiveCommentPostedEventHandler(bool isSuccess);
	public delegate void NicoLiveCommentRecievedEventHandler(Chat chat);
	public delegate void NicoLiveCommentServerConnectedEventHandler();

	public delegate void NicoLiveHeartbeatEventHandler(uint commentCount, uint watchCount);

	public delegate void NicoLiveEndConnectedEventHandler();


	// detail see@ http://dic.nicovideo.jp/a/%E3%83%8B%E3%82%B3%E3%83%8B%E3%82%B3%E7%94%9F%E6%94%BE%E9%80%81%20%E9%81%8B%E5%96%B6%E3%82%B3%E3%83%9E%E3%83%B3%E3%83%89%E3%81%BE%E3%81%A8%E3%82%81
	public enum NicoLiveOperationCommandType
	{
		Play,
		PlaySound,

		PermanentDisplay,
		ClearPermanentDisplay,

		// 投票
		Vote,

		// コメントの表示位置
		CommentMode,

		// ニコニコ実況の公式チャンネルのコメントを下に流す
		Call,

		// ニコニコ実況の表示を消す
		Free,

		// プレイヤーを再起動
		Reset,

		// 運営からのお知らせ
		Info, 

		// バックステージパスユーザーのコメント
		Press,

		Disconnect,

		Koukoku,

		Telop,

		Hidden,
		CommentLock,

		Ignore, 
	}

	public class NicoLiveOperationCommandEventArgs
	{
		public Chat Chat { get; set; }
		public NicoLiveOperationCommandType CommandType { get; set; }
		public string[] Arguments { get; set; }
	}

	public delegate void NicoLiveOperationCommandEventHanlder(NicoLiveCommentClient sender, NicoLiveOperationCommandEventArgs args);

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

		CancellationTokenSource _LiveCommentRecieveCancelSource;
		Task _LiveCommentRecievingTask;

		AsyncLock _NetworkStreamLock = new AsyncLock();
		NetworkStream _NetworkStream;



		public event NicoLiveCommentServerConnectedEventHandler CommentServerConnected;

		public event NicoLiveCommentRecievedEventHandler CommentRecieved;
		public event NicoLiveOperationCommandEventHanlder OperationCommandRecieved;

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

			_LiveCommentRecieveCancelSource = new CancellationTokenSource();
			using (var releaser = await _NetworkStreamLock.LockAsync())
			{
				_NetworkStream = _Client.GetStream();

				// TCP接続による受信タスクを開始
				_LiveCommentRecievingTask = DataRecivingTask();

				// コメントサーバーに接続開始データを送信
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
				_LiveCommentRecieveCancelSource?.Cancel();

				await Task.Delay(250);

				_LiveCommentRecieveCancelSource?.Dispose();
				_LiveCommentRecieveCancelSource = null;

				// 生成元となったTcpClientをDisposeすると一緒にDisposeされる
				_NetworkStream = null;
			}

			_LiveCommentRecievingTask = null;

			await ExitHeartbeatTimer();
		}


		public async Task PostComment(string comment, uint userId, string command)
		{
			var postkey = await NiconicoContext.Live.GetPostKeyAsync(ThreadIdNumber, CommentCount / 100);
			if (string.IsNullOrEmpty(postkey))
			{
				CommentPosted?.Invoke(false /* failed post comment */);
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
			catch (Exception ex)
			{
				Debug.WriteLine(ex.ToString());

				CommentPosted?.Invoke(false /* failed post comment */);
			}
		}



		public void Dispose()
		{
			Stop()
				.ContinueWith((prevTask) => _Client.Dispose())
				.ConfigureAwait(false);
		}






		private async Task DataRecivingTask()
		{
			if (_NetworkStream == null ) { return; }

			Debug.WriteLine("start comment recieve.");

			try
			{
				bool isEndConnect = false;
				while (!isEndConnect)
				{
					// データが来るまで待つ
					while (true)
					{
						_LiveCommentRecieveCancelSource.Token.ThrowIfCancellationRequested();

						using (var releaser = await _NetworkStreamLock.LockAsync())
						{
							if (_NetworkStream.DataAvailable)
							{
								break;
							}
						}

						_LiveCommentRecieveCancelSource.Token.ThrowIfCancellationRequested();

						await Task.Delay(50);
					}

					// 受信したデータをバッファに読み込む
					using (var releaser = await _NetworkStreamLock.LockAsync())
					{
						isEndConnect = await _NetworkStream.ReadAsync(_Buffer, 0, _Buffer.Length) == 0;
					}

					_LiveCommentRecieveCancelSource.Token.ThrowIfCancellationRequested();

					if (!isEndConnect)
					{
						// バッファに詰め込まれた文字列を解析する
						var recievedRawString = Encoding.UTF8.GetString(_Buffer);

						Debug.Write($"recieve comment data -> ");

						var nullStrStart = recievedRawString.IndexOf('\0');
						var trimEmptyString = recievedRawString.Remove(nullStrStart);
						ParseLiveCommentServerResponse(trimEmptyString);

						Debug.WriteLine($" -> end");

						Array.Clear(_Buffer, 0, _Buffer.Length);
					}					
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.ToString());
			}

			Debug.WriteLine("exit comment recieve.");

			IsCommentServerConnected = false;
		}


		private void ParseLiveCommentServerResponse(string recievedString)
		{
			if (string.IsNullOrWhiteSpace(recievedString))
			{
				Debug.Write($"IGNORE");
				return;
			}

			if (!recievedString.StartsWith("<") || !recievedString.EndsWith(">"))
			{
				// Note: 寄り厳密にXMLフォーマットチェックをやるなら
				// <>の数が同数であることをチェックする
				Debug.Write($"illigal format, required XML");
				return;
			}

			var xmlDoc = XDocument.Parse(recievedString);
			var xmlRoot = xmlDoc.Root;
			var elementName = xmlRoot.Name.LocalName;
			Debug.Write(elementName);
			Debug.Write(" -> ");
			if (elementName == "thread")
			{
				Debug.Write("connect success");
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
				// <chat_result status="{コメント投稿要求の返答}" />
				var result = xmlRoot.Attribute(XName.Get("status")).Value;

				/*
				0 = 投稿に成功した
				1 = 投稿に失敗した(短時間に同じ内容のコメントを投稿しようとした、パラメータが間違っている、他)
				4 = 投稿に失敗した(ごく短時間にコメントを連投しようとした、パラメータが間違っている、他)
					*/

				Debug.Write(result);
				Debug.Write(result == "0" ? " (success)" : " (failed)");

				CommentPosted?.Invoke(result == "0");
			}
			else if (elementName == "chat")
			{

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
							Debug.Write(chat.Text);

							OperationCommnad officialCommand = null;
							string[] officialCommandArguments = null;
							if (ChcekOfficialOperationComment(chat, out officialCommand, out officialCommandArguments))
							{
								var args = new NicoLiveOperationCommandEventArgs()
								{
									CommandType = officialCommand.CommandType,
									Arguments = officialCommandArguments,
									Chat = chat
								};

								OperationCommandRecieved?.Invoke(this, args);
							}
							else
							{
								CommentRecieved?.Invoke(chat);
							}
						}
					}
				}
				catch { }
			}
			else
			{
				Debug.WriteLine($"not supproted");
				Debug.Write(" -> ");
				Debug.Write(recievedString);
			}

		}


		class OperationCommnad
		{
			public NicoLiveOperationCommandType CommandType { get; set; }
			public string Text { get; set; }

			public OperationCommnad(string text, NicoLiveOperationCommandType type)
			{
				CommandType = type;
				Text = text;
			}
		}

		private static OperationCommnad[] OfficialCommands = new[] 
		{
			new OperationCommnad("play", NicoLiveOperationCommandType.Play),
			new OperationCommnad("playsound", NicoLiveOperationCommandType.PlaySound),
			new OperationCommnad("perm", NicoLiveOperationCommandType.PermanentDisplay),
			new OperationCommnad("cls", NicoLiveOperationCommandType.ClearPermanentDisplay),
			new OperationCommnad("clear", NicoLiveOperationCommandType.ClearPermanentDisplay),
			new OperationCommnad("vote", NicoLiveOperationCommandType.Vote),
			new OperationCommnad("commentmode", NicoLiveOperationCommandType.CommentMode),
			new OperationCommnad("call", NicoLiveOperationCommandType.Call),
			new OperationCommnad("free", NicoLiveOperationCommandType.Free),
			new OperationCommnad("disconnect", NicoLiveOperationCommandType.Disconnect),
			new OperationCommnad("reset", NicoLiveOperationCommandType.Reset),
			new OperationCommnad("info", NicoLiveOperationCommandType.Info),
			new OperationCommnad("press", NicoLiveOperationCommandType.Press),
			new OperationCommnad("commentlock", NicoLiveOperationCommandType.CommentLock),
			new OperationCommnad("koukoku", NicoLiveOperationCommandType.Koukoku),

			new OperationCommnad("keepalive", NicoLiveOperationCommandType.Ignore),
		};

		private static bool ChcekOfficialOperationComment(Chat chat, out OperationCommnad command, out string[] arguments)
		{
			// TODO: 運営コメントを許可された人の判定


			var chatText = chat.Text;
			// hiddenだけ / で開始していないため例外的に先に判断
			if (chatText == "hidden")
			{			
				command = new OperationCommnad("hidden", NicoLiveOperationCommandType.Hidden);
				arguments = new string[0];
				return true;
			}
			if (!chatText.StartsWith("/"))
			{
				command = null;
				arguments = null;
				return false;
			}

			var maybeCommandText = new string(chatText.Skip(1).TakeWhile(x => x != ' ').ToArray());

			command = OfficialCommands.SingleOrDefault(x => x.Text == maybeCommandText);


			
			if (command.CommandType == NicoLiveOperationCommandType.Ignore)
			{
				command = null;
				arguments = null;
				return false;
			}

			bool hasCommand = command != null;

			if (hasCommand)
			{
				// Note: ダブルクォーテーションを含む空白区切りの文字列を分解して
				// argumentsに代入します

				// ダブルクォーテーションのセットが不完全な場合は
				// コマンド自体を無効と判定し、falseを返します

				var argumentCandidates = chatText.Split(' ').Skip(1);

				List<string> finalArguments = new List<string>();
				List<string> unclosedStrings = new List<string>();

				foreach (var str in argumentCandidates)
				{
					var isOpenString = str.StartsWith("\"");
					var isCloseString = str.EndsWith("\"");
					
					if (!(isOpenString ^ isCloseString))
					{
						// 00 or 11の場合はワンフレーズで文字列が閉じていると判断
						finalArguments.Add(str.Trim('\"'));
					}
					else if (isOpenString)
					{
						if (unclosedStrings.Count > 0)
						{
							arguments = null;
							return false;
						}

						unclosedStrings.Add(str);
					}
					else //if (isCloseString)
					{
						if (unclosedStrings.Count == 0)
						{
							arguments = null;
							return false;
						}

						unclosedStrings.Add(str);

						var joined = string.Join(" ", unclosedStrings);

						finalArguments.Add(joined.Trim('\"'));

						unclosedStrings.Clear();
					}
				}

				if (unclosedStrings.Count > 0)
				{
					arguments = null;
					return false;
				}

				arguments = finalArguments.ToArray();
			}
			else
			{
				arguments = null;
			}

			return hasCommand;
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

	/// <summary>
	/// ニコニコ生放送での送信用コメントデータ<br />
	/// コメントの送信は、このクラスをXMLにシリアライズしてTcpClientを通して送信されます。
	/// </summary>
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
