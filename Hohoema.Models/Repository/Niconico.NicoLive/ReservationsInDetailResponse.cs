using Hohoema.Models.Repository.Niconico.NicoLive;
using System.Collections.Generic;
using System.Linq;

namespace Hohoema.Models.Repository.Niconico.NicoLive
{
    public sealed class ReservationsInDetailResponse
    {
        private readonly Mntone.Nico2.Live.ReservationsInDetail.ReservationsInDetailResponse _res;

        public ReservationsInDetailResponse(Mntone.Nico2.Live.ReservationsInDetail.ReservationsInDetailResponse res)
        {
            _res = res;
        }

        IReadOnlyList<TimeshiftProgram> _ReservedProgram;
        public IReadOnlyList<TimeshiftProgram> ReservedProgram => _ReservedProgram ??= _res.ReservedProgram.Select(x => new TimeshiftProgram(x)).ToList();
    }
}
