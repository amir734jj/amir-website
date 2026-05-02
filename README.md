# Amir Hesamian – Personal Website

A C# Razor-based static site generator. Content is authored in YAML files with typed C# models, rendered through Razor templates, and output as static HTML.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) or [Docker](https://www.docker.com/)

## Run locally

```bash
cd RazorBlogGenerator
dotnet run
```

Output is written to `RazorBlogGenerator/dist/`.

You can also specify a custom output path:

```bash
dotnet run -- /path/to/output
```

## Run with Docker

```bash
docker build -t amir-website ./RazorBlogGenerator
docker run --rm -v ${PWD}/dist:/output amir-website /output
```

Output is written to `dist/` in the repo root.

## Project structure

```
RazorBlogGenerator/
├── Data/
│   ├── site.yaml          # Site config (name, navbar, footer)
│   ├── resume.yaml        # CV page content
│   ├── blog-index.yaml    # Blog listing page
│   └── Posts/             # Blog posts (markdown)
├── Models/                # C# types for YAML deserialization
├── Templates/             # Razor (.cshtml) templates
│   ├── Layout.cshtml      # Shared HTML shell
│   ├── Resume.cshtml      # CV template
│   ├── BlogIndex.cshtml   # Blog listing template
│   └── Post.cshtml        # Blog post template
├── SiteGenerator.cs       # Convention-based generator
└── Program.cs             # Entry point
```

## Adding content

Each `.yaml` file in `Data/` declares its C# model, Razor template, slug, and title:

```yaml
model: PostModel
template: Post.cshtml
slug: posts/my-new-post/
title: "My New Post"
published_on: "2026-05-01"
excerpt: "A short summary."
content_type: markdown
---
# Markdown content goes here
```

Site-wide settings (navbar links, site name, footer) are in `Data/site.yaml`.
