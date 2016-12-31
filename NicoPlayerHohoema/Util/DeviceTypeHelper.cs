using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Util
{
    public static class DeviceTypeHelper
    {
        public static bool IsXbox
        {
            get
            {
                return Microsoft.Toolkit.Uwp.Helpers.SystemInformation.DeviceFamily.EndsWith("Xbox");
            }
        }

        public static bool IsDesktop
        {
            get
            {
                return Microsoft.Toolkit.Uwp.Helpers.SystemInformation.DeviceFamily.EndsWith("Desktop");
            }
        }

        public static bool IsMobile
        {
            get
            {
                return Microsoft.Toolkit.Uwp.Helpers.SystemInformation.DeviceFamily.EndsWith("Mobile");
            }
        }

        public static bool IsIot
        {
            get
            {
                return Microsoft.Toolkit.Uwp.Helpers.SystemInformation.DeviceFamily.EndsWith("Iot");
            }
        }
    }
}
