using Prism.Events;
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


        public static InAppNotificationPayload CreateRegistrationResultNotification(
            Mntone.Nico2.ContentManageResult registrationResult,
            string containerKindLabel,
            string containerTitle,
            string targetTitle,
            params InAppNotificationCommand[] commands
            )
        {
            string notifyContent = null;
            if (registrationResult == Mntone.Nico2.ContentManageResult.Success)
            {
                notifyContent = $"{containerKindLabel}に登録完了\n「{containerTitle}」に「{targetTitle}」を追加しました";
            }
            else if (registrationResult == Mntone.Nico2.ContentManageResult.Exist)
            {
                notifyContent = $"{containerKindLabel}に既に追加済み\n「{containerTitle}」に「{targetTitle}」を追加済みです";
            }
            else
            {
                notifyContent = $"{containerKindLabel}に登録失敗\n「{containerTitle}」に「{targetTitle}」を追加できませんでした";
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
