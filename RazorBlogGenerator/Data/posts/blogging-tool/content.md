## Background

I have used C# and Razor (cshtml) extensively and have always enjoyed the experience. Blazor largely replaced the classic MVC/Razor model, but its ecosystem still feels limited compared to React or Angular - fewer quality libraries and awkward integration with existing JavaScript tooling. But that is a topic for another post.

## What I Wanted

I wanted a file-system-based blogging platform with the following properties:

- Markdown for post content, YAML for metadata
- Extensible via new `.cshtml` templates and C# model classes
- JSON Schema validation for YAML files, so editors provide IntelliSense and catch type mismatches early
- Case-insensitive URLs
- Generated HTML with proper metadata tags for search engines
- A development mode that watches for file changes and regenerates the static output automatically

No existing tool matched all of these requirements.

## Building It

Over a weekend, I built exactly this. The source is available at [github.com/amir734jj/amir-website](https://github.com/amir734jj/amir-website). If you need a lightweight blogging setup, you can drop in your own content without touching any of the infrastructure.

Under the hood it uses:

- [RazorLight](https://github.com/toddams/RazorLight) - Razor templates to HTML, without needing a full ASP.NET runtime
- [Markdig](https://github.com/xoofx/markdig) - Markdown to HTML
- [NJsonSchema](https://github.com/RicoSuter/NJsonSchema) - derives JSON schemas from C# classes and validates YAML against them

## Why Not Jekyll or Hugo?

I tried Jekyll back in 2017. The command surface is large and the Ruby dependency chain gets in the way. Hugo is powerful but overkill for a simple Markdown blog - the configuration model alone takes time to learn.

I wanted something that gets out of my way so I can focus on writing. Paired with a small static file server and a CI pipeline that rebuilds and pushes a container image on every commit, this setup does exactly that.
