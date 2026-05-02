# Amir Hesamian – Personal Website

A C# Razor-based static site generator. Content is authored in YAML files with typed C# models, rendered through Razor templates, and output as static HTML.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) or [Docker](https://www.docker.com/)

## Run locally

```bash
cd RazorBlogGenerator
dotnet run -- build
```

Output is written to `RazorBlogGenerator/dist/`.

You can also specify a custom output path:

```bash
dotnet run -- build -o /path/to/output
```

### Other commands

```bash
dotnet run -- schema       # Generate JSON schemas from C# models
dotnet run -- validate     # Validate YAML files against schemas
dotnet run -- --help       # Show all commands
```

## Run with Docker

```bash
docker build -t amir-website ./RazorBlogGenerator
docker run -p 8080:80 amir-website
```

Site is served at `http://localhost:8080`. The Docker build validates YAML, generates static HTML, and serves it with nginx.

## Project structure

```
RazorBlogGenerator/
├── Data/
│   ├── site.yaml              # Site config (name, navbar, footer)
│   ├── resume.yaml            # CV page content
│   ├── blog-index.yaml        # Blog listing page
│   └── Posts/                 # Blog posts
│       ├── my-post.yaml       # Post metadata (schema-validated)
│       └── my-post.md         # Post markdown content
├── Schemas/                   # Auto-generated JSON schemas
├── Models/                    # C# types for YAML deserialization
├── Templates/                 # Razor (.cshtml) templates
│   ├── Layout.cshtml          # Shared HTML shell
│   ├── Resume.cshtml          # CV template
│   ├── BlogIndex.cshtml       # Blog listing template
│   └── Post.cshtml            # Blog post template
├── SiteGenerator.cs           # Convention-based generator
├── SchemaGenerator.cs         # JSON schema generator
├── YamlValidator.cs           # YAML validation against schemas
└── Program.cs                 # Entry point (CommandLineParser)
```

## Adding content

### Blog post

Create a `.yaml` and `.md` file pair in `Data/Posts/`:

**`Data/Posts/my-new-post.yaml`**
```yaml
# $schema: ../../Schemas/post.schema.json
model: PostModel
template: Post.cshtml
slug: posts/my-new-post/
title: "My New Post"
published_on: "2026-05-01"
excerpt: "A short summary."
content_type: markdown
```

**`Data/Posts/my-new-post.md`**
```markdown
# My New Post

Content goes here.
```

### New page type

1. Create a C# class extending `ContentPage` with `[SchemaName]`
2. Create a `.cshtml` template
3. Add a `.yaml` file in `Data/`
4. Run `dotnet run -- schema` to regenerate schemas

Site-wide settings (navbar links, site name, footer) are in `Data/site.yaml`.
