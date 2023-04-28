#nullable enable
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Helpers;
using Hohoema.Models.Application;
using System;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Hohoema.ViewModels.Community;

public class CommunityNewsViewModel
{
    public static async Task<CommunityNewsViewModel> Create(
        string communityId,
        string title,
        string authorName,
        DateTime postAt,
        string contentHtml,
        AppearanceSettings appearanceSettings
            )
    {
        ApplicationTheme appTheme;
        if (appearanceSettings.ApplicationTheme == ElementTheme.Dark)
        {
            appTheme = ApplicationTheme.Dark;
        }
        else if (appearanceSettings.ApplicationTheme == ElementTheme.Light)
        {
            appTheme = ApplicationTheme.Light;
        }
        else
        {
            appTheme = Views.Helpers.SystemThemeHelper.GetSystemTheme();
        }

        var id = $"{communityId}_{postAt.ToString("yy-MM-dd-H-mm")}";
        var uri = await HtmlFileHelper.PartHtmlOutputToCompletlyHtml(id, contentHtml, appTheme);
        return new CommunityNewsViewModel(communityId, title, authorName, postAt, uri);
    }

    private CommunityNewsViewModel(
        string communityId,
        string title,
        string authorName,
        DateTime postAt,
        Uri htmlUri
        )
    {
        CommunityId = communityId;
        Title = title;
        AuthorName = authorName;
        PostAt = postAt;
        ContentHtmlFileUri = htmlUri;
    }

    public string CommunityId { get; private set; }
    public string Title { get; private set; }
    public string AuthorName { get; private set; }
    public DateTime PostAt { get; private set; }
    public Uri ContentHtmlFileUri { get; private set; }

    private RelayCommand<Uri>? _ScriptNotifyCommand;
    public RelayCommand<Uri> ScriptNotifyCommand => _ScriptNotifyCommand ??= new RelayCommand<Uri>((parameter) =>
    {
        if (parameter == null) { return; }
        System.Diagnostics.Debug.WriteLine($"script notified: {parameter}");

        _ = Ioc.Default.GetRequiredService<IMessenger>().OpenUriAsync(parameter);
    });

}
