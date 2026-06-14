using RazorBlogGenerator.Models.Attributes;

namespace RazorBlogGenerator.Models;

[ContentSchema]
public class ProjectsModel : ContentPage
{
    public string Description { get; set; } = string.Empty;
    
    public List<ProjectData> Projects { get; set; } = [];
    public List<LibraryData> Libraries { get; set; } = [];
}
