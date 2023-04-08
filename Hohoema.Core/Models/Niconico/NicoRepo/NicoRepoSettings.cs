using Hohoema.Infra;
using NiconicoToolkit.NicoRepo;
using System.Collections.Generic;

namespace Hohoema.Models.Niconico.NicoRepo;

public class NicoRepoSettings : FlagsRepositoryBase
{
    [System.Obsolete]
    public NicoRepoSettings()
    {
        DisplayNicoRepoMuteContextTriggers = Read(new List<NicoRepoMuteContextTrigger>(), nameof(DisplayNicoRepoMuteContextTriggers));
    }

    private List<NicoRepoMuteContextTrigger> _DisplayNicoRepoMuteContextTriggers;

    [System.Obsolete]
    public List<NicoRepoMuteContextTrigger> DisplayNicoRepoMuteContextTriggers
    {
        get => _DisplayNicoRepoMuteContextTriggers;
        set => SetProperty(ref _DisplayNicoRepoMuteContextTriggers, value);
    }
}
