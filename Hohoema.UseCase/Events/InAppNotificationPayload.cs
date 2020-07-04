﻿using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.UI.Xaml.Controls;
using I18NPortable;
using Hohoema.Models.Repository;
using Hohoema.UseCase.Services;

namespace Hohoema.UseCase.Events
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


        public static InAppNotificationPayload CreateRegistrationResultNotification(
            ContentManageResult registrationResult,
            string containerKindLabel,
            string containerTitle,
            string targetTitle,
            params InAppNotificationCommand[] commands
            )
        {
            string notifyContent = null;
            if (registrationResult == ContentManageResult.Success)
            {
                notifyContent = "CompleteRegisrationForKind0_AddItem2_ToKindTitle1".Translate(containerKindLabel, containerKindLabel, targetTitle);
            }
            else if (registrationResult == ContentManageResult.Exist)
            {
                notifyContent = "ExistRegisrationForKind0_AddItem2_ToKindTitle1".Translate(containerKindLabel, containerKindLabel, targetTitle);
            }
            else
            {
                notifyContent = "FailedRegisrationForKind0_AddItem2_ToKindTitle1".Translate(containerKindLabel, containerKindLabel, targetTitle);
            }

            return new InAppNotificationPayload()
            {
                Content = notifyContent,
                ShowDuration = TimeSpan.FromSeconds(7),
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
