﻿#nullable enable
namespace Hohoema.Models.Niconico.Follow.LoginUser;

public class CommunituFollowAdditionalInfo
{
    /// <summary>
    /// コミュニティ登録申請のタイトル（メールのサブジェクト）
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// コミュニティ登録申請のコメント文（メールの本文）
    /// </summary>
    public string Comment { get; set; }

    /// <summary>
    /// 申請可否の結果を受け取るか
    /// </summary>
    public bool Notify { get; set; }
}
