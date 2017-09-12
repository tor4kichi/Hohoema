﻿using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.UI.Xaml.Controls;
using Microsoft.Practices.Unity;

namespace NicoPlayerHohoema.Models
{
    public class InAppNotificationEvent : PubSubEvent<InAppNotificationPayload>
    {
    }

    public class InAppNotificationDismissEvent : PubSubEvent<long>
    {
    }


    public sealed class InAppNotificationPayload
    {
        public Symbol? SymbolIcon { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool IsShowDismissButton { get; set; } = true;
        public TimeSpan? ShowDuration { get; set; }
        public List<InAppNotificationCommand> Commands { get; set; } = new List<InAppNotificationCommand>();

        readonly static TimeSpan MinShowDuration = TimeSpan.FromSeconds(3);
        public static InAppNotificationPayload CreateReadOnlyNotification(
            string content,
            TimeSpan? showDuration = null,
            Symbol? symbolIcon = null
            )
        {
            if (showDuration == null || showDuration <= MinShowDuration)
            {
                showDuration = MinShowDuration;
            }

            return new InAppNotificationPayload()
            {
                Content = content,
                SymbolIcon = symbolIcon,
                ShowDuration = showDuration,
                IsShowDismissButton = true
            };
        }


        public static InAppNotificationPayload CreateIntractRequireNotification(
            string content,
            params InAppNotificationCommand[] commands
            )
        {
            return new InAppNotificationPayload()
            {
                Content = content,
                IsShowDismissButton = false,
                Commands = commands.ToList()
            };
        }

    }

    public class InAppNotificationCommand
    {
        public string Label { get; set; }
        public ICommand Command { get; set; }
    }
}
