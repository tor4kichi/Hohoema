using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.ViewModels.PlayerSidePaneContent
{
    public class EmptySidePaneContentViewModel : SidePaneContentViewModelBase
    {
        public static EmptySidePaneContentViewModel Default { get; } = new EmptySidePaneContentViewModel();

        private EmptySidePaneContentViewModel() { }
    }
}
