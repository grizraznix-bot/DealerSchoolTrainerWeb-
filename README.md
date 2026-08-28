# Dealer School Trainer — Web

Web (Blazor WebAssembly) version of the Dealer School Trainer desktop
app, hosted on GitHub Pages. Same idea as the desktop trainers, but
runs entirely in the browser — no install, no antivirus warnings, no
platform-specific build.

## Status

This is the initial skeleton: a minimal page proving the whole
pipeline works (local build → GitHub → automatic deployment → live on
Pages). None of the actual games are built yet — that's the next
phase, one game at a time, same pattern as the desktop app.

## Project type

This is a **separate project from the desktop `CasinoTrainers` repo**,
not a branch or a folder inside it. Different tech stack (Blazor
WebAssembly, not WinForms), different build system (`dotnet publish`
produces a static site here, not an .exe), and deliberately kept
independent so nothing about this can affect the desktop app's own
history or tags.

## How deployment works

Every push to `main` triggers `.github/workflows/deploy.yml`
automatically:
1. Builds the project (`dotnet publish`)
2. Fixes the page's `<base href>` to match this repo's name (GitHub
   Pages serves from a subpath, not the domain root — without this
   fix every asset link would break once deployed, even though it
   works fine testing locally)
3. Adds a `.nojekyll` file (GitHub Pages runs Jekyll by default,
   which ignores any folder starting with an underscore — exactly
   what Blazor's own `_framework` folder is named; without this the
   whole site fails to load once deployed)
4. Publishes the result to GitHub Pages

You never need to manually deploy anything — commit and push, and the
live site updates itself within a minute or two.

## One-time setup (only needs doing once, ever)

After pushing this project to GitHub for the first time:
1. Go to the repository's **Settings → Pages**
2. Under "Build and deployment" → "Source", select **GitHub Actions**
   (not "Deploy from a branch")
3. That's it — the workflow above handles everything from here on

## Local development

Requires the .NET SDK (same version as the desktop project). From
this folder:

```
dotnet run
```

Opens a local dev server, usually at `https://localhost:5001` or
similar (the exact URL is printed in the console).
