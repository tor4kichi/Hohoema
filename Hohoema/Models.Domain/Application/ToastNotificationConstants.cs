using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.Application
{
    public static class ToastNotificationConstants
    {
        public const string ToastArgumentKey_Id = "id";
        public const string ToastArgumentKey_Action = "action";
        public const string ToastArgumentValue_Action_Play = "play";
        public const string ToastArgumentValue_Action_Delete = "delete";

        public const string ProgressBarBindableValueKey_ProgressValue = "progressValue";
        public const string ProgressBarBindableValueKey_ProgressValueOverrideString = "progressValueString";
        public const string ProgressBarBindableValueKey_ProgressStatus = "progressStatus";
    }
}
