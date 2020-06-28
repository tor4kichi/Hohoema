﻿using NicoPlayerHohoema.Interfaces;
using NicoPlayerHohoema.Models.Subscriptions;
using NicoPlayerHohoema.Repository.Subscriptions;
using NicoPlayerHohoema.UseCase.Playlist;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.Extensions;

namespace NicoPlayerHohoema.UseCase.Subscriptions
{
    public sealed class FeedResultAddToWatchLater
    {
        private readonly SubscriptionManager _subscriptionManager;
        private readonly HohoemaPlaylist _hohoemaPlaylist;
        private readonly SubscriptionSettingsRepository _subscriptionSettingsRepository;

        List<SubscriptionFeedUpdateResult> Results = new List<SubscriptionFeedUpdateResult>();

        public FeedResultAddToWatchLater(
            SubscriptionManager subscriptionManager,
            HohoemaPlaylist hohoemaPlaylist,
            SubscriptionSettingsRepository subscriptionSettingsRepository
            )
        {
            _subscriptionManager = subscriptionManager;
            _hohoemaPlaylist = hohoemaPlaylist;
            _subscriptionSettingsRepository = subscriptionSettingsRepository;
            Observable.FromEventPattern<SubscriptionFeedUpdateResult>(
                h => _subscriptionManager.Updated += h,
                h => _subscriptionManager.Updated -= h
                )
                .Select(x => x.EventArgs)
                .Do(x => Results.Add(x))
                .Throttle(TimeSpan.FromSeconds(5))
                .Subscribe(_ => 
                {
                    var items = Results.ToList();
                    Results.Clear();

                    if (!_subscriptionSettingsRepository.IsSortWithSubscriptionUpdated)
                    {
                        foreach (var newVideo in items.SelectMany(x => x.NewVideos))
                        {
                            _hohoemaPlaylist.AddWatchAfterPlaylist(newVideo);

                            Debug.WriteLine("[FeedResultAddToWatchLater] added: " + newVideo.Label);
                        }
                    }
                    else
                    {
                        // 注意: SubscriptionManager.Updatedイベントの挙動に依存した動作になる
                        // 新着動画が無くても購読を上から順番に全部更新し通知するという仕様に依存する
                        // キチンとやるなら 新着の無い購読ソースの動画が歯抜けにならぬよう SubscriptionManager.GetAllSubscriptionInfo() を利用して埋める必要がある

                        var backup = _hohoemaPlaylist.WatchAfterPlaylist.ToImmutableArray();
                        var watchAfterItems = _hohoemaPlaylist.WatchAfterPlaylist.ToDictionary(x => x.Id);
                        _hohoemaPlaylist.WatchAfterPlaylist.ClearOnScheduler();
                        
                        List<Database.NicoVideo> videos = new List<Database.NicoVideo>();
                        foreach (var subscUpdateResult in items)
                        {
                            // あとで見るに購読から追加済みのものを切り分けてリスト化
                            List<Database.NicoVideo> unsortedVideos = new List<Database.NicoVideo>();
                            foreach (var video in subscUpdateResult.Videos)
                            {
                                if (watchAfterItems.Remove(video.Id, out var item))
                                {
                                    unsortedVideos.Add(video);
                                }
                            }

                            // 新着動画も加えて
                            unsortedVideos.AddRange(subscUpdateResult.NewVideos);

                            if (subscUpdateResult.Entity.SourceType == SubscriptionSourceType.User)
                            {
                                // 購読ソース毎の動画リストに対するソートを実行
                                // 1. タイトルの類似度によるグループ化
                                // 2. 類似度グループ内でのPostAtによる昇順ソート
                                // 3. 各類似度グループの個数による昇順ソート

                                const double TitleSimilarlityThreshold = 0.60;

                                // Note: 類似度のしきい値について
                                // 0.9ぐらい高くしてしまうと、例えばパート投稿してるシリーズの「サブタイトル」部分の変化で類似判定から漏れてしまいます
                                // 逆に低すぎると「【ゆっくり実況】xxxxxのxxxx partYY」といったタイトルの他パート動画と同じ定型部分によって類似判定が通ってしまいます
                                
                                var nearByTitleMap = new Dictionary<string, List<Database.NicoVideo>>();
                                foreach (var video in unsortedVideos)
                                {
                                    bool isDetected = false;
                                    foreach (var heroTitle in nearByTitleMap.Keys)
                                    {
                                        if (Models.Helpers.StringHelper.CalculateSimilarity(heroTitle, video.Title) >= TitleSimilarlityThreshold)
                                        {
                                            nearByTitleMap[heroTitle].Add(video);
                                            isDetected = true;
                                            break;
                                        }
                                    }

                                    if (!isDetected)
                                    {
                                        nearByTitleMap.Add(video.Title, new List<Database.NicoVideo>() { video });
                                    }
                                }

                                videos.AddRange(nearByTitleMap.Values.OrderBy(x => x.Count).SelectMany(x => x.OrderBy(x => x.PostedAt)));
                            }
                            else
                            {
                                videos.AddRange(unsortedVideos.OrderBy(x => x.PostedAt));
                            }
                            
                        }

                        // ここまでで watchAfterItems には購読ソースに関連しない動画のみが残ってる

                        // 購読ソース毎タイトルでソート済みの動画 と あとで見るに追加されていたが購読ソースと関連しない動画 を連結してあとで見るに追加
                        try
                        {
                            _hohoemaPlaylist.WatchAfterPlaylist.AddRangeOnScheduler(
                                videos.Concat(watchAfterItems.Values)
                                );
                        }
                        catch
                        {
                            _hohoemaPlaylist.WatchAfterPlaylist.AddRangeOnScheduler(backup);
                        }
                    }
                    
                });

        }

    }
}
