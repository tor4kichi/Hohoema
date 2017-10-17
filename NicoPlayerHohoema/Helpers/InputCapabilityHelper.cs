using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Input;

namespace NicoPlayerHohoema.Helpers
{
    public sealed class InputCapabilityHelper
    {
        public static bool IsMouseCapable
        {
            get
            {
                MouseCapabilities mouseCapabilities = new Windows.Devices.Input.MouseCapabilities();
                return mouseCapabilities.NumberOfButtons > 0;
            }
        }

        public static bool IsTouchCapable
        {
            get
            {
                TouchCapabilities touchCapabilities = new Windows.Devices.Input.TouchCapabilities();
                return touchCapabilities.TouchPresent != 0;
            }
        }

        public static bool IsKeyboardCapable
        {
            get
            {
                KeyboardCapabilities keyboardCapabilities = new Windows.Devices.Input.KeyboardCapabilities();
                return keyboardCapabilities.KeyboardPresent != 0;
            }
        }
    }
}
