namespace RazorBlogGenerator.Models;

public class PdfOptions
{
    public string Output { get; set; } = null!;
    public string? FontFamily { get; set; }
    public List<string> FontFiles { get; set; } = [];
}