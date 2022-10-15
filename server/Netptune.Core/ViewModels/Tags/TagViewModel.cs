namespace Netptune.Core.ViewModels.Tags;

public class TagViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string OwnerName { get; set; } = null!;

    public string OwnerId { get; set; } = null!;
}
