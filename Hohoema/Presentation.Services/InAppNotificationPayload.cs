using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.UI.Xaml.Controls;
using Unity;
using I18NPortable;
using Microsoft.Toolkit.Mvvm.Messaging.Messages;

namespace Hohoema.Presentation.Services
{
    public class InAppNotificationEvent : ValueChangedMessage<InAppNotificationPayload>
    {
        public InAppNotificationEvent(InAppNotificationPayload value) : base(value)
        {
        }
    }

    public class InAppNotificationDismissEvent : ValueChangedMessage<long>
    {
        public InAppNotificationDismissEvent() : base(0)
        {
        }
    }


    public sealed class InAppNotificationPayload
    {
        public string Title { get; set; }
        public string Icon { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool IsShowDismissButton { get; set; } = true;
        public TimeSpan? ShowDuration { get; set; }
        public List<InAppNotificationCommand> Commands { get; set; } = new List<InAppNotificationCommand>();

        readonly static TimeSpan MinShowDuration = TimeSpan.FromSeconds(3);
        public static InAppNotificationPayload CreateReadOnlyNotification(
            string content,
            TimeSpan? showDuration = null
            )
        {
            if (showDuration == null || showDuration <= MinShowDuration)
            {
                showDuration = MinShowDuration;
            }

            return new InAppNotificationPayload()
            {
                Content = content,
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
                notifyContent = "CompleteRegisrationForKind0_AddItem2_ToKindTitle1".Translate(containerKindLabel, containerTitle, targetTitle);
            }
            else if (registrationResult == Mntone.Nico2.ContentManageResult.Exist)
            {
                notifyContent = "ExistRegisrationForKind0_AddItem2_ToKindTitle1".Translate(containerKindLabel, containerTitle, targetTitle);
            }
            else
            {
                notifyContent = "FailedRegisrationForKind0_AddItem2_ToKindTitle1".Translate(containerKindLabel, containerTitle, targetTitle);
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
