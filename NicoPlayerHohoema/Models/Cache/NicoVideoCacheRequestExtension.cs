namespace NicoPlayerHohoema.Models.Cache
{
    public static class NicoVideoCacheRequestExtension
    {
        public static NicoVideoCacheState ToCacheState(this NicoVideoCacheRequest req)
        {
            if (req is NicoVideoCacheInfo) { return NicoVideoCacheState.Cached; }
            else if (req is NicoVideoCacheProgress) { return NicoVideoCacheState.Downloading; }
            else { return NicoVideoCacheState.Pending; }
        }
    }
}
