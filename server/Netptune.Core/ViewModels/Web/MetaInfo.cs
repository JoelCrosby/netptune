namespace Netptune.Core.ViewModels.Web
{
    public class MetaInfo
    {
        public bool HasData { get; set; }

        public string SiteName { get; set; }

        public string Url { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public string Keywords { get; set; }

        public MetaInfoImage Image { get; set; } = new MetaInfoImage();
    }

    public class MetaInfoImage
    {
        public string Url { get; set; }
    }
}
