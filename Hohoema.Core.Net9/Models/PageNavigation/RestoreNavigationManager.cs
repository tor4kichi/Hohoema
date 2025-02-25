﻿#nullable enable

using Hohoema.Infra;
using Hohoema.Models.Playlist;
using NiconicoToolkit.Video;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Hohoema.Models.PageNavigation;

public sealed class RestoreNavigationManager
{
    private static readonly NavigationStackRepository _navigationStackRepository;

    
    static RestoreNavigationManager()
    {
        _navigationStackRepository ??= new NavigationStackRepository();
    }

    public RestoreNavigationManager()
    {
    }

    
    public void SetCurrentPlayerEntry(PlayerEntry entry)
    {
        _navigationStackRepository.SetCurrentPlayerEntry(entry);
    }

    public void SetCurrentPlayerEntry(VideoId videoId, TimeSpan position, PlaylistItemsSourceOrigin? playlistOrigin, string? playlistId)
    {
        _navigationStackRepository.SetCurrentPlayerEntry(new PlayerEntry(videoId, position, playlistOrigin, playlistId));
    }


    public void ClearCurrentPlayerEntry()
    {
        _navigationStackRepository.ClearCurrentPlayerEntry();
    }

    
    public PlayerEntry? GetCurrentPlayerEntry()
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
        return entries.Any(x => string.IsNullOrWhiteSpace(x.PageName))
            ? throw new ArgumentNullException()
            : _navigationStackRepository.SetBackNavigationEntriesAsync(entries.ToArray());
    }

    
    public Task SetForwardNavigationEntriesAsync(IEnumerable<PageEntry> entries)
    {
        return entries.Any(x => string.IsNullOrWhiteSpace(x.PageName))
            ? throw new ArgumentNullException()
            : _navigationStackRepository.SetForwardNavigationEntriesAsync(entries.ToArray());
    }

    
    public Task<PageEntry[]> GetBackNavigationEntriesAsync()
    {
        return _navigationStackRepository.GetBackNavigationEntriesAsync();
    }

    
    public Task<PageEntry[]> GetForwardNavigationEntriesAsync()
    {
        return _navigationStackRepository.GetForwardNavigationEntriesAsync();
    }

    public class NavigationStackRepository : FlagsRepositoryBase
    {
        
        public NavigationStackRepository()
        {

        }

        public const string CurrentPlayerEntryName = "CurrentPlayerEntry";
        public const string CurrentNavigationEntryName = "CurrentNavigationEntry";
        public const string BackNavigationEntriesName = "BackNavigationEntries";
        public const string ForwardNavigationEntriesName = "ForwardNavigationEntries";
        private readonly JsonSerializerOptions _options = new()
        {
            Converters =
            {
                new JsonTimeSpanConverter(),
            }
        };

        
        public PlayerEntry? GetCurrentPlayerContent()
        {
            byte[]? json = Read<byte[]?>(null, CurrentPlayerEntryName);
            return json == null ? null : JsonSerializer.Deserialize<PlayerEntry>(json, _options);
        }

        
        public void SetCurrentPlayerEntry(PlayerEntry entry)
        {
            byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(entry, _options);
            Save(bytes, CurrentPlayerEntryName);
        }

        
        public void ClearCurrentPlayerEntry()
        {
            Save<PlayerEntry>(null, CurrentPlayerEntryName);
        }

        
        public PageEntry GetCurrentNavigationEntry()
        {
            byte[] json = Read<byte[]>(null, CurrentNavigationEntryName);
            return json == null ? null : JsonSerializer.Deserialize<PageEntry>(json);
        }

        
        public void SetCurrentNavigationEntry(PageEntry entry)
        {
            byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(entry);
            Save(bytes, CurrentNavigationEntryName);
        }

        
        public async Task<PageEntry[]> GetBackNavigationEntriesAsync()
        {
            byte[] json = await ReadFileAsync<byte[]>(null, BackNavigationEntriesName);
            return json == null ? (new PageEntry[0]) : JsonSerializer.Deserialize<PageEntry[]>(json);
        }

        
        public async Task SetBackNavigationEntriesAsync(PageEntry[] entries)
        {
            byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(entries);
            _ = await SaveFileAsync(bytes, BackNavigationEntriesName);
        }

        
        public async Task<PageEntry[]> GetForwardNavigationEntriesAsync()
        {
            byte[] json = await ReadFileAsync<byte[]>(null, ForwardNavigationEntriesName);
            return json == null ? (new PageEntry[0]) : JsonSerializer.Deserialize<PageEntry[]>(json);
        }

        
        public async Task SetForwardNavigationEntriesAsync(PageEntry[] entries)
        {
            byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(entries);
            _ = await SaveFileAsync(bytes, ForwardNavigationEntriesName);
        }
    }
}

public class PlayerEntry
{
    public PlayerEntry() 
    {
        ContentId = "";
    }

    public PlayerEntry(string contentId, TimeSpan position, PlaylistItemsSourceOrigin? playlistOrigin, string? playlistId)
    {
        ContentId = contentId;
        Position = position;
        PlaylistOrigin = playlistOrigin;
        PlaylistId = playlistId;
    }

     public string ContentId { get; set; }

    public TimeSpan Position { get; set; }

    public PlaylistItemsSourceOrigin? PlaylistOrigin { get; set; }
    public string? PlaylistId { get; set; }
}


public class PageEntry
{
    public PageEntry() { }

    public PageEntry(string pageName, IEnumerable<KeyValuePair<string, object>> parameters)
    {
        PageName = pageName;
        Parameters = parameters?.ToDictionary(x => x.Key, (x) => x.Value?.ToString()).ToList();
    }

    public string PageName { get; set; }
    public List<KeyValuePair<string, string>> Parameters { get; set; }
}
