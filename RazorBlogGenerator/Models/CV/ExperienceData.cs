namespace RazorBlogGenerator.Models.CV;

public class ExperienceData
{
    public string Company { get; set; } = null!;
    public string Location { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Period { get; set; } = null!;
    public List<RoleData> Roles { get; set; } = [];
}