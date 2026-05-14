using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax.Inlines;

namespace RazorBlogGenerator.MarkdownExtensions;

/// <summary>
/// Markdig extension that resolves <c>{{key}}</c> tokens in markdown content
/// against a per-page variable dictionary.
/// </summary>
public sealed class VarsExtension(Dictionary<string, string> vars) : IMarkdownExtension
{
    public void Setup(MarkdownPipelineBuilder pipeline)
    {
        if (!pipeline.InlineParsers.Contains<VarsInlineParser>())
        {
            pipeline.InlineParsers.Insert(0, new VarsInlineParser());
        }
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
        if (renderer is HtmlRenderer htmlRenderer &&
            !htmlRenderer.ObjectRenderers.Contains<VarsInlineRenderer>())
        {
            htmlRenderer.ObjectRenderers.Insert(0, new VarsInlineRenderer(vars));
        }
    }
}

/// <summary>AST node representing a <c>{{key}}</c> token.</summary>
internal sealed class VarsInline(string key) : LeafInline
{
    public string Key { get; } = key;
}

/// <summary>
/// Parses <c>{{key}}</c> tokens into <see cref="VarsInline"/> nodes.
/// Resolution is intentionally deferred to the renderer so the parser
/// stays stateless and reusable across pages.
/// </summary>
internal sealed class VarsInlineParser : InlineParser
{
    public VarsInlineParser()
    {
        OpeningCharacters = ['{'];
    }

    public override bool Match(InlineProcessor processor, ref StringSlice slice)
    {
        // Require exactly "{{" to open
        if (slice.CurrentChar != '{' || slice.PeekChar(1) != '{')
        {
            return false;
        }

        var line = slice;
        line.NextChar(); // skip first  {
        line.NextChar(); // skip second {

        var keyStart = line.Start;

        while (line.CurrentChar != '\0')
        {
            if (line.CurrentChar == '}' && line.PeekChar(1) == '}')
            {
                var key = line.Text.Substring(keyStart, line.Start - keyStart).Trim();
                line.NextChar(); // skip first  }
                line.NextChar(); // skip second }
                slice = line;
                processor.Inline = new VarsInline(key);
                return true;
            }

            line.NextChar();
        }

        // No closing `}}` found — leave the opening `{` for other parsers
        return false;
    }
}

/// <summary>
/// Renders a <see cref="VarsInline"/> node by looking up the key in the vars dictionary.
/// Falls back to the original <c>{{key}}</c> literal if the key is not defined.
/// </summary>
internal sealed class VarsInlineRenderer(Dictionary<string, string> vars) : HtmlObjectRenderer<VarsInline>
{
    protected override void Write(HtmlRenderer renderer, VarsInline obj)
    {
        if (vars.TryGetValue(obj.Key, out var value))
        {
            renderer.WriteEscape(value);
        }
        else
        {
            renderer.Write("{{").Write(obj.Key).Write("}}");
        }
    }
}
