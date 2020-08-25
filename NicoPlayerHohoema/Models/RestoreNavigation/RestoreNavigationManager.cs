using NicoPlayerHohoema.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models.RestoreNavigation
{
    public sealed class RestoreNavigationManager
    {
        private readonly NavigationStackRepository _navigationStackRepository;

        public RestoreNavigationManager()
        {
            _navigationStackRepository = new NavigationStackRepository();
        }

        public void SetCurrentNavigationEntry(PageEntry pageEntry)
        {
            _navigationStackRepository.SetCurrentNavigationEntry(pageEntry);
        }

        public PageEntry GetCurrentNavigationEntry()
        {
            return _navigationStackRepository.GetCurrentNavigationEntry();
        }

        public Task SetBackNavigationEntriesAsync(IEnumerable<PageEntry> entries)
        {
            return _navigationStackRepository.SetBackNavigationEntriesAsync(entries.ToArray());
        }

        public Task SetForwardNavigationEntriesAsync(IEnumerable<PageEntry> entries)
        {
            return _navigationStackRepository.SetForwardNavigationEntriesAsync(entries.ToArray());
        }

        public Task<PageEntry[]> GetBackNavigationEntriesAsync()
        {
            return _navigationStackRepository.GetBackNavigationEntriesAsync();
        }

        public Task<PageEntry[]> GetForwardNavigationEntriesAsync()
        {
            return _navigationStackRepository.GetForwardNavigationEntriesAsync();
        }



        internal class NavigationStackRepository : FlagsRepositoryBase
        {
            public NavigationStackRepository()
            {

            }

            public const string CurrentNavigationEntryName = "CurrentNavigationEntry";
            public const string BackNavigationEntriesName = "BackNavigationEntries";
            public const string ForwardNavigationEntriesName = "ForwardNavigationEntries";


            public PageEntry GetCurrentNavigationEntry()
            {
                var json = Read<byte[]>(null, CurrentNavigationEntryName);
                if (json == null) { return null; }
                return JsonSerializer.Deserialize<PageEntry>(json);
            }

            public void SetCurrentNavigationEntry(PageEntry entry)
            {
                var bytes = JsonSerializer.SerializeToUtf8Bytes(entry);
                Save(bytes, CurrentNavigationEntryName);
            }

            public async Task<PageEntry[]> GetBackNavigationEntriesAsync()
            {
                var json = await ReadFileAsync<byte[]>(null, BackNavigationEntriesName);
                if (json == null) { return new PageEntry[0]; }
                return JsonSerializer.Deserialize<PageEntry[]>(json);
            }

            public async Task SetBackNavigationEntriesAsync(PageEntry[] entries)
            {
                var bytes = JsonSerializer.SerializeToUtf8Bytes(entries);
                await SaveFileAsync(bytes, BackNavigationEntriesName);
            }

            public async Task<PageEntry[]> GetForwardNavigationEntriesAsync()
            {
                var json = await ReadFileAsync<byte[]>(null, ForwardNavigationEntriesName);
                if (json == null) { return new PageEntry[0]; }
                return JsonSerializer.Deserialize<PageEntry[]>(json);
            }

            public async Task SetForwardNavigationEntriesAsync(PageEntry[] entries)
            {
                var bytes = JsonSerializer.SerializeToUtf8Bytes(entries);
                await SaveFileAsync(bytes, ForwardNavigationEntriesName);
            }
        }
    }

    public class PageEntry
    {
        public PageEntry() { }

        public PageEntry(string pageName, IEnumerable<KeyValuePair<string, object>> parameters)
        {
            PageName = pageName;
            Parameters = parameters?.ToDictionary(x => x.Key, (x) => x.Value.ToString()).ToList();
        }

        public string PageName { get; set; }
        public List<KeyValuePair<string, string>> Parameters { get; set; }
    }
}
