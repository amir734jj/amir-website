namespace RazorBlogGenerator.Models.CV;

public class EducationData
{
    public string School { get; set; } = null!;
    public string Location { get; set; } = null!;
    public string Degree { get; set; } = null!;
    public string Period { get; set; } = null!;
    public string? ThesisUrl { get; set; }
    public List<string> Details { get; set; } = [];
}