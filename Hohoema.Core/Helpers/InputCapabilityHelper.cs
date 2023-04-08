using Windows.Devices.Input;

namespace Hohoema.Helpers;

public sealed class InputCapabilityHelper
{
    public static bool IsMouseCapable
    {
        get
        {
            MouseCapabilities mouseCapabilities = new();
            return mouseCapabilities.NumberOfButtons > 0;
        }
    }

    public static bool IsTouchCapable
    {
        get
        {
            TouchCapabilities touchCapabilities = new();
            return touchCapabilities.TouchPresent != 0;
        }
    }

    public static bool IsKeyboardCapable
    {
        get
        {
            KeyboardCapabilities keyboardCapabilities = new();
            return keyboardCapabilities.KeyboardPresent != 0;
        }
    }
}
