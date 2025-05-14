using System;

namespace CMonitorAdministration
{
    public class ImageInfo
    {
        public string Name { get; set; } //blob name
        public DateTime CapturedAtUtc { get; set; }
        public MemoryStream? Image { get; set; }

        public ImageInfo()
        {
            Name = "";
            Image = null;
        }
    }
}