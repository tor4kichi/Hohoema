# Subscription、購読機能の設計概念について

Hohoema的にはフィード機能の後継に当たるのが「購読機能」です。

フィード機能に足りなかった新着通知機能を入れたかったのと、更新と通知の処理系を分離できてなかった点を踏まえて作り直しています。

更新の実処理はSubscriptionManagerクラスが、通知と更新タイミング管理はWatchItLaterクラスが担当しています。


* SubscriptionManager
  * Subscriptions (Subscription)
    * Label (string)
	* IsEnabled (bool)
    * Sources (SubscriptionSource)
	* Destinations (SubscriptionDestination)

## WatchItLater

購読結果の自動更新管理と必要に応じてユーザーへの通知を行います。

SubscriptionManagerを一方的に利用する立場であり、購読機能におけるアプリケーションサイドのディレクター的な役割です。

## SubscriptionManager

購読周りの事務的な処理を担当。Subscriptionとその永続化を管理しますが、UI的なことは関知しません。

* Subscriptionや購読結果情報のローカルDatabaseへのセーブ＆ロード
* Subscriptionに基づいた購読結果情報の取得と時系列の並び替え

`GetSubscriptionFeedResultAsObservable` を呼び出して、返り値をSubscribe 等でObservableシーケンスをホットにすることで購読結果取得の処理が走ります。

SubscriptionUpdateInfoのNewFeedsの動画から視聴履歴で得られる動画を差し引いて、未視聴な新着動画を構成して新着通知できるとベターかと思います。

ローカルDBへの保存時にはSubscriptionであればSubscriptionData、SubscriptionSourceであればSubscriptionSourceData、などシリアライズオブジェクトへの変換を行うようにしています。

詳しくは `Hohoema.Database.Local.Subscription.SubscriptionDb` を確認してください。

## Subscription

購読情報。購読ソースと追加先プレイリスト、および購読情報のユーザー管理用の情報（ラベルや有効無効フラグ）を持ちます。

## SubscriptionSource

購読ソース。ユーザー動画一覧、チャンネル動画一覧、キーワードやタグの検索結果、などを対象とします。

## SubscriptionDestination

追加先プレイリスト。「あとで見る」をデフォルトで指定。

他にも任意のローカルマイリストやログインユーザーのマイリスト（とりあえずマイリストなど）も指定できます。








