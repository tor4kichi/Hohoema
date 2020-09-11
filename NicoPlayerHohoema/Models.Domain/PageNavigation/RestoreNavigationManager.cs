
using Hohoema.Models.Domain.Playlist;
using Hohoema.Models.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.PageNavigation
{
    public sealed class RestoreNavigationManager
    {
        private readonly NavigationStackRepository _navigationStackRepository;

        public RestoreNavigationManager()
        {
            _navigationStackRepository = new NavigationStackRepository();
        }


        public void SetCurrentPlayerEntry(PlayerEntry entry)
        {
            _navigationStackRepository.SetCurrentPlayerEntry(entry);
        }

        public void ClearCurrentPlayerEntry()
        {
            _navigationStackRepository.ClearCurrentPlayerEntry();
        }

        public PlayerEntry GetCurrentPlayerEntry()
        {
            return _navigationStackRepository.GetCurrentPlayerContent();
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
            if (entries.Any(x => string.IsNullOrWhiteSpace(x.PageName)))
            {
                throw new ArgumentNullException();
            }

            return _navigationStackRepository.SetBackNavigationEntriesAsync(entries.ToArray());
        }

        public Task SetForwardNavigationEntriesAsync(IEnumerable<PageEntry> entries)
        {
            if (entries.Any(x => string.IsNullOrWhiteSpace(x.PageName)))
            {
                throw new ArgumentNullException();
            }

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

            public const string CurrentPlayerEntryName = "CurrentPlayerEntry";
            public const string CurrentNavigationEntryName = "CurrentNavigationEntry";
            public const string BackNavigationEntriesName = "BackNavigationEntries";
            public const string ForwardNavigationEntriesName = "ForwardNavigationEntries";

            JsonSerializerOptions _options = new JsonSerializerOptions()
            { 
                Converters = 
                {
                    new JsonTimeSpanConverter(),
                }
            };
            public PlayerEntry GetCurrentPlayerContent()
            {
                var json = Read<byte[]>(null, CurrentPlayerEntryName);
                if (json == null) { return null; }
                return JsonSerializer.Deserialize<PlayerEntry>(json, _options);
            }

            public void SetCurrentPlayerEntry(PlayerEntry entry)
            {
                var bytes = JsonSerializer.SerializeToUtf8Bytes(entry, _options);
                Save(bytes, CurrentPlayerEntryName);
            }

            public void ClearCurrentPlayerEntry()
            {
                Save<PlayerEntry>(null, CurrentPlayerEntryName);
            }


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

    public class PlayerEntry
    {
        public string ContentId { get; set; }
        
        public TimeSpan Position { get; set; }

        public PlaylistOrigin? PlaylistOrigin { get; set; }
        public string PlaylistId { get; set; }
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
