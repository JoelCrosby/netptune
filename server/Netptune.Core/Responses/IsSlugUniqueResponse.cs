namespace Netptune.Core.Responses
{
    public class IsSlugUniqueResponse
    {
        public string Request { get; set; }

        public string Slug { get; set; }

        public bool IsUnique { get; set; }
    }
}
