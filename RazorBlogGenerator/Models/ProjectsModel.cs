using RazorBlogGenerator.Models.Attributes;

namespace RazorBlogGenerator.Models;

[ContentSchema]
public class ProjectsModel : ContentPage
{
    public List<ProjectData> Projects { get; set; } = [];
}
