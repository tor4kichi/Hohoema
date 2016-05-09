using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace NicoPlayerHohoema.Models
{
	public static class VideoDescriptionHelper
	{
		

		public static async Task<Uri> PartHtmlOutputToCompletlyHtml(string videoId, string descriptionHtml)
		{
			// Note: WebViewに渡すHTMLファイルをテンポラリフォルダを経由してアクセスします。
			// WebView.Sourceの仕様上、テンポラリフォルダにサブフォルダを作成し、そのサブフォルダにコンテンツを配置しなければなりません。

			const string VideDescHTMLFolderName = "VideoDesctiptionHTML";
			// ファイルとして動画説明HTMLを書き出す
			var outputFolder = await ApplicationData.Current.TemporaryFolder.CreateFolderAsync(VideDescHTMLFolderName, CreationCollisionOption.OpenIfExists);




			string descJoinedHtmlText = "";

			// ファイルのテンプレートになるHTMLテキストを取得して
			var templateHtmlFileStorage = await StorageFile.GetFileFromApplicationUriAsync(
				new Uri("ms-appx:///Assets/VideoDescription/VideoDescription.html")
				);

			// テンプレートHTMLに動画説明を埋め込んだテキストを作成
			using (var stream = await templateHtmlFileStorage.OpenAsync(FileAccessMode.Read))
			using (var textReader = new StreamReader(stream.AsStream()))
			{
				var templateText = textReader.ReadToEnd();
				descJoinedHtmlText = templateText
					.Replace("{Description}", descriptionHtml)
					.Replace("http://", "https://");
			}


			// テンポラリストレージ空間に動画説明HTMLファイルを書き込み
			var filename = videoId + ".html";
			var savedVideoDescHtmlFile = await outputFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
			using (var stream = await savedVideoDescHtmlFile.OpenStreamForWriteAsync())
			using (var writer = new StreamWriter(stream))
			{
				writer.Write(descJoinedHtmlText);
			}

			var folderName = Path.GetFileName(outputFolder.Path);

			// 
			return new Uri($"ms-appdata:///temp/{VideDescHTMLFolderName}/{filename}");
		}
	}
}
