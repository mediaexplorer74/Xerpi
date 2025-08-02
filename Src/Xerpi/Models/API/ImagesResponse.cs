using System;

namespace Xerpi.Models.API
{
    public class ImagesResponse
    {
        public ApiImage[] Images { get; set; } = Array.Empty<ApiImage>();
    }
}
