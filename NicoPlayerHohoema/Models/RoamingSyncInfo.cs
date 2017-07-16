using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{
    [DataContract]
    public class RoamingSyncInfo
    {
        [DataMember]
        public List<FileSyncInfo> SyncInfoItems { get; set; } = new List<FileSyncInfo>();

        public void AddOrReplace(string relativeFilePath, DateTime updateAt)
        {
            var item = SyncInfoItems.FirstOrDefault(x => x.RelativeFilePath == relativeFilePath);

            if (item != null)
            {
                SyncInfoItems.Remove(item);
            }

            SyncInfoItems.Add(new FileSyncInfo()
            {
                RelativeFilePath = relativeFilePath,
                UpdateAt = updateAt,
                Mode = SyncMode.Modify
            });
        }

        public void Remove(string relativeFilePath)
        {
            var item = SyncInfoItems.FirstOrDefault(x => x.RelativeFilePath == relativeFilePath);

            if (item != null)
            {
                item.Mode = SyncMode.Remove;
            }
        }
    }

    [DataContract]
    public class FileSyncInfo
    {
        [DataMember]
        public string RelativeFilePath { get; set; }

        [DataMember]
        public DateTime UpdateAt { get; set; }

        [DataMember]
        public SyncMode Mode { get; set; }
    }


    public enum SyncMode
    {
        Modify,
        Remove,
    }

}
