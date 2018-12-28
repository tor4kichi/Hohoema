using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Gaming.Input;

namespace NicoPlayerHohoema.Views
{
    [Flags]
    public enum UINavigationButtons
    {
        None = 0x00000000,

        Menu = 0x00000001,
        View = 0x00000002,
        Accept = 0x00000004,
        Cancel = 0x00000008,

        Up = 0x00000010,
        Down = 0x00000020,
        Left = 0x00000040,
        Right = 0x00000080,

        Context1 = 0x00000100,
        Context2 = 0x00000200,
        Context3 = 0x00000400,
        Context4 = 0x00000800,

        PageUp = 0x00001000,
        PageDown = 0x00002000,
        PageLeft = 0x00004000,
        PageRight = 0x00008000,

        ScrollUp = 0x00010000,
        ScrollDown = 0x00020000,
        ScrollLeft = 0x00040000,
        ScrollRight = 0x00080000,
    }

    public static class UINavigationButtonsExtention
    {
        static public RequiredUINavigationButtons ToRequiredButtons(this UINavigationButtons kind)
        {
            var val = (int)kind & 0x000000FF;
            return (RequiredUINavigationButtons)val;
        }

        static public OptionalUINavigationButtons ToOptionalButtons(this UINavigationButtons kind)
        {
            var val = ((int)kind & 0x000FFF00) >> 8;
            return (OptionalUINavigationButtons)val;
        }
    }

    public static class RequiredUINavigationButtonsHelper
    {
        static public UINavigationButtons ToUINavigationButtons(RequiredUINavigationButtons buttons)
        {
            return (UINavigationButtons)buttons;
        }
    }

    public static class OptionalUINavigationButtonsHelper
    {
        static public UINavigationButtons ToUINavigationButtons(OptionalUINavigationButtons buttons)
        {
            return (UINavigationButtons) ((int)buttons << 8);
        }
    }
}
