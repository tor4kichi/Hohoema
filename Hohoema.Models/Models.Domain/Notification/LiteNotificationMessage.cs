﻿using Microsoft.Toolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Controls;

namespace Hohoema.Models.Domain.Notification
{
    public enum DisplayDuration
    {
        Default,
        MoreAttention,
    }

    public class LiteNotificationPayload
    {
        public bool IsDisplaySymbol { get; set; }

        public Symbol Symbol { get; set; }
        public string Content { get; set; }

        /// <summary>
        /// 表示の長さ。Durationが設定されている場合は無視される。
        /// </summary>
        public DisplayDuration? DisplayDuration { get; set; }

        /// <summary>
        /// 表示の長さ。直接指定。
        /// </summary>
        public TimeSpan? Duration { get; set; }
    }

    public sealed class LiteNotificationMessage : ValueChangedMessage<LiteNotificationPayload>
    {
        public LiteNotificationMessage(LiteNotificationPayload value) : base(value)
        {
        }
    }
}
