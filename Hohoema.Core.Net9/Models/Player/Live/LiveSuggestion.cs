﻿#nullable enable
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hohoema.Models.Live;

public class LiveSuggestion
{
    public string Title { get; private set; }

    public List<SuggestAction> Actions { get; private set; }

    public LiveSuggestion(string title, params SuggestAction[] actions)
    {
        Title = title;
        Actions = actions.ToList();
    }
}

public class SuggestAction
{
    public string Label { get; private set; }
    public RelayCommand SuggestActionCommand { get; private set; }

    public SuggestAction(string label, Action action)
    {
        Label = label;
        SuggestActionCommand = new RelayCommand(action);
    }
}

/*
	public static class LiveSuggestionExtention
	{
		public static LiveSuggestion Make(this LiveStatusType liveStatus, NicoLiveVideo liveVideo, Services.PageManager pageManager, NiconicoSession niconicoSession)
		{
			string title = liveStatus.ToString();

			List<SuggestAction> actions = new List<SuggestAction>();

			switch (liveStatus)
			{
				case LiveStatusType.NotFound:
					title = "放送が見つかりませんでした";
					break;
				case LiveStatusType.Closed:
					title = "放送は終了しました";
					break;
				case LiveStatusType.ComingSoon:
					title = "放送はもうすぐ始まります";
					break;
				case LiveStatusType.Maintenance:
					title = "現在メンテナンス中です";
					break;
				case LiveStatusType.CommunityMemberOnly:
					title = "この放送はコミュニティメンバー限定です";
					actions.Add(new SuggestAction("コミュニティページを開く", () => 
					{
						pageManager.OpenPageWithId(HohoemaPageType.Community, liveVideo.BroadcasterCommunityId);
					}));
					break;
				case LiveStatusType.Full:
					title = "満員です";
					break;
				case LiveStatusType.PremiumOnly:
					title = "この放送はプレミアム会員限定です";
					break;
				case LiveStatusType.NotLogin:
					title = "視聴するには「ニコニコ」にログインが必要です";
                
                break;
				default:
					break;
			}


			return new LiveSuggestion(title, actions.ToArray());
		}
	}
	*/
