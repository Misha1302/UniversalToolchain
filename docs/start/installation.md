---
title: Installation
description: Show how to clone, build, and prepare the project locally.
---

# Installation

This page shows how to prepare the repository for Wist usage, .NET development and local documentation work.

## When to read this page

Read this page before running examples or editing the documentation site.

## Goal

Verify the repository branch, build the .NET solution and prepare the VitePress documentation site.

## Prerequisites

- Git.
- .NET SDK `10.0.103` or a compatible SDK accepted by `UniversalToolchain/global.json`.
- Node.js and npm for VitePress documentation.
- Repository branch: `docs-site`.

The current validation baseline is .NET 10 and target framework `net10.0`. Older target frameworks are not the current compatibility target.

## Steps

### 1. Check the branch

From the repository root:

```bash
git status
git branch --show-current
```

Expected branch:

```text
docs-site
```

If the branch is not `docs-site`, switch branches before editing documentation.

### 2. Restore and build the .NET solution

```bash
dotnet restore UniversalToolchain/Wist.sln
dotnet build UniversalToolchain/Wist.sln -c Release --no-restore
```

For quick local work, `dotnet build` from the repository root may also work, but the solution path is the documented validation path.

### 3. Run tests when changing behavior

For documentation-only changes, tests may not be necessary. For code, module, dialect or runtime changes, run the relevant test projects:

```bash
dotnet test UniversalToolchain/Tests/Tests.csproj -c Release --no-build
dotnet test UniversalToolchain/UniversalToolchain.Modules.Tests/UniversalToolchain.Modules.Tests.csproj -c Release --no-build
dotnet test UniversalToolchain/UniversalToolchain.Dialects.Tests/UniversalToolchain.Dialects.Tests.csproj -c Release --no-build
```

### 4. Install documentation dependencies

```bash
npm install
```

### 5. Run the documentation site locally

```bash
npm run docs:dev
```

This starts the VitePress development server for the `docs/` directory.

### 6. Build the documentation site

```bash
npm run docs:build
```

This must pass before merging documentation changes.

## Expected result

After completing these steps:

- the .NET solution builds;
- the Wist CLI can run examples;
- the VitePress documentation site can be served locally;
- `npm run docs:build` produces a static documentation build without broken internal links.

## What happened internally

The .NET build compiles UniversalToolchain, Wist, shipped modules, dialect infrastructure, CLI projects and tests. The npm commands install and run VitePress against the `docs/` folder. These are separate workflows: .NET validates the framework, VitePress validates the documentation site.

## Common mistakes

- Running commands from inside `docs/` instead of the repository root.
- Using `master` or `main` instead of `docs-site`.
- Installing an older .NET SDK that cannot build `net10.0` projects.
- Forgetting `npm install` before `npm run docs:dev` or `npm run docs:build`.
- Treating docs build success as runtime validation. Documentation build does not replace .NET tests.

## Next

Continue with [First Program](/start/first-program).
