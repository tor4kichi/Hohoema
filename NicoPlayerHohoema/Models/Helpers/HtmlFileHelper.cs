using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace NicoPlayerHohoema.Models.Helpers
{
	public static class HtmlFileHelper
	{
		public static async Task<Uri> PartHtmlOutputToCompletlyHtml(string id, string htmlFragment, Windows.UI.Xaml.ApplicationTheme theme)
		{
			// Note: WebViewに渡すHTMLファイルをテンポラリフォルダを経由してアクセスします。
			// WebView.Sourceの仕様上、テンポラリフォルダにサブフォルダを作成し、そのサブフォルダにコンテンツを配置しなければなりません。

			const string VideDescHTMLFolderName = "html";
			// WebViewで表示可能なHTMLに変換
			string htmlText = await ToCompletlyHtmlAsync(htmlFragment, theme);

			// テンポラリストレージ空間に動画説明HTMLファイルを書き込み
			var filename = id + ".html";
			var outputFolder = await ApplicationData.Current.TemporaryFolder.CreateFolderAsync(VideDescHTMLFolderName, CreationCollisionOption.OpenIfExists);
			var savedVideoDescHtmlFile = await outputFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
			await FileIO.WriteTextAsync(savedVideoDescHtmlFile, htmlText);

			return new Uri($"ms-appdata:///temp/{VideDescHTMLFolderName}/{filename}");
		}


		public static async Task<string> ToCompletlyHtmlAsync(string htmlFragment, Windows.UI.Xaml.ApplicationTheme theme)
		{
			// ファイルのテンプレートになるHTMLテキストを取得して
			var templateHtmlFileStorage = await StorageFile.GetFileFromApplicationUriAsync(
				new Uri("ms-appx:///Assets/html/template.html")
				);

			// テンプレートHTMLに動画説明を埋め込んだテキストを作成
			var templateText = await FileIO.ReadTextAsync(templateHtmlFileStorage);
			return templateText
				.Replace("{content}", htmlFragment)
				.Replace("http://", "https://")
				.Replace("{foreground-color}", theme == Windows.UI.Xaml.ApplicationTheme.Dark ? "#EFEFEF" : "000000");
		}
	}
}
