# Amir Hesamian – Personal Website

A C# Razor-based static site generator. Content is authored in YAML files with typed C# models, rendered through Razor templates, and output as static HTML.

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
dotnet run -- watch              # Watch for changes, rebuild, and serve at http://localhost:8080
dotnet run -- watch -p 3000      # Custom port
dotnet run -- schema             # Generate JSON schemas from C# models
dotnet run -- validate           # Validate YAML files against schemas
dotnet run -- --help             # Show all commands
```

## Run with Docker

```bash
docker build -t amir-website .
docker run -p 8080:80 amir-website
```

Site is served at `http://localhost:8080`. The Docker build validates YAML, generates static HTML, and serves it with nginx.

## Adding content

### Blog post

Create a folder in `Data/posts/` with `index.yaml` and `content.md`:

**`Data/posts/my-new-post/index.yaml`**
```yaml
# $schema: ../../../Schemas/post-model.schema.json
model: PostModel
template: Post.cshtml
title: "My New Post"
published_on: "2026-05-01"
excerpt: "A short summary."
tags:
  - example
```

**`Data/posts/my-new-post/content.md`**
```markdown
# My New Post

Content goes here. Images can be co-located:

![Diagram](diagram.png)
```

### Folder routing

Routes are derived from the folder structure — no `slug` field needed:
- `Data/index.yaml` → `/`
- `Data/cv/index.yaml` → `/cv/`
- `Data/posts/my-post/index.yaml` → `/posts/my-post/`

Assets (images, etc.) placed alongside content are copied to the same route in the output.

### New page type

1. Create a C# class extending `ContentPage` with `[SchemaName]`
2. Create a `.cshtml` template
3. Add a folder with `index.yaml` in `Data/`
4. Run `dotnet run -- schema` to regenerate schemas

Site-wide settings (navbar links, site name, footer) are in `Data/site.yaml`.
