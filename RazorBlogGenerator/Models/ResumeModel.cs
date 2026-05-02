namespace RazorBlogGenerator.Models;

[SchemaName]
public class ResumeModel : ContentPage
{
    public string Name { get; set; } = null!;
    public string Tagline { get; set; } = null!;
    public ContactData Contact { get; set; } = null!;
    public List<EducationData> Education { get; set; } = [];
    public List<SkillData> Skills { get; set; } = [];
    public List<ExperienceData> Experience { get; set; } = [];
    public List<string> Honors { get; set; } = [];
    public List<string> Activities { get; set; } = [];
    public string WorkAuthorization { get; set; } = null!;
}