using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Helpers
{
    public static class DeviceTypeHelper
    {
        private static bool _IsXbox = Microsoft.Toolkit.Uwp.Helpers.SystemInformation.DeviceFamily.EndsWith("Xbox");
        public static bool IsXbox => _IsXbox;

        private static bool _IsDesktop = Microsoft.Toolkit.Uwp.Helpers.SystemInformation.DeviceFamily.EndsWith("Desktop");
        public static bool IsDesktop => _IsDesktop;

        private static bool _IsMobile = Microsoft.Toolkit.Uwp.Helpers.SystemInformation.DeviceFamily.EndsWith("Mobile");
        public static bool IsMobile => _IsMobile;

        private static bool _IsIot = Microsoft.Toolkit.Uwp.Helpers.SystemInformation.DeviceFamily.EndsWith("Iot");
        public static bool IsIot => _IsIot;
    }
}
