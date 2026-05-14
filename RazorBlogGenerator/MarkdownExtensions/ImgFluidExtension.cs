using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Renderers.Html.Inlines;
using Markdig.Syntax.Inlines;

namespace RazorBlogGenerator.MarkdownExtensions;

internal sealed class ImgFluidExtension : IMarkdownExtension
{
    public void Setup(MarkdownPipelineBuilder pipeline) { }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
        if (renderer is HtmlRenderer htmlRenderer)
        {
            var existing = htmlRenderer.ObjectRenderers.FindExact<LinkInlineRenderer>();
            if (existing != null)
            {
                htmlRenderer.ObjectRenderers.Remove(existing);
            }

            htmlRenderer.ObjectRenderers.AddIfNotAlready(new ImgFluidLinkInlineRenderer());
        }
    }
}

internal sealed class ImgFluidLinkInlineRenderer : LinkInlineRenderer
{
    protected override void Write(HtmlRenderer renderer, LinkInline link)
    {
        if (link.IsImage)
        {
            link.GetAttributes().AddClass("img-fluid");
        }

        base.Write(renderer, link);
    }
}
