using System;

namespace Xerpi.Models.API
{
    public class ImageSearchResponse
    {
        public ApiImage[] Images { get; set; } = Array.Empty<ApiImage>();
        public uint Total { get; set; }
    }
}
