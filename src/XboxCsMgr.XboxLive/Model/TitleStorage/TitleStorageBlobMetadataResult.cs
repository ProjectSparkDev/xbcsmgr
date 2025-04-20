using System.Collections.Generic;

namespace XboxCsMgr.XboxLive.Model.TitleStorage
{
    public class TitleStorageBlobMetadataResult
    {
        public IList<TitleStorageBlobMetadata> Blobs { get; set; }
        public TitleStorageBlobMetadataResultpagingInfo pagingInfo { get; set; }
    }
    public class TitleStorageBlobMetadataResultpagingInfo
    {
        public int totalItems { get; set; }
        public string continuationToken { get; set; }
    }
}
