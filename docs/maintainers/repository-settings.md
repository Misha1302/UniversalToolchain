---
title: Repository Settings and Launch Checklist
description: Canonical GitHub metadata, discoverability settings, legacy-repository routing and launch sequence for Wist.
---

# Repository Settings and Launch Checklist

This checklist keeps the GitHub surface aligned with the current public alpha. Repository settings are not versioned by a normal pull request, so maintainers should apply the values below in GitHub after merging the launch-surface change.

## Canonical GitHub About text

Use this repository description:

> Restricted formulas for .NET: validate rules and compile approved formulas into typed delegates.

Set the project website to:

> https://misha1302.github.io/Wist2/

## Topics

Use the following topics, ordered from product intent to implementation detail:

```text
dotnet
csharp
rule-engine
expression-engine
embedded-dsl
business-rules
compiler
interpreter
cil
runtime
language-tooling
ssa
```

## Social preview

1. Open **Settings → General → Social preview**.
2. Export `docs/assets/wist-social-preview.svg` as a `1280×640` PNG.
3. Upload the PNG as the repository social preview.
4. Verify the preview in a private message or link-preview debugger before the public launch.

The same artwork is embedded near the top of `readme.md`, so the repository remains visually understandable even before the setting is applied.

## Canonical repository routing

`Misha1302/Wist2` is the active repository until an intentional rename is planned and redirects are verified.

For historical repositories (`Wist`, `Wist4`, `Wist5`, and `Wist2Msil`):

1. add a prominent deprecation README that links to `Misha1302/Wist2`;
2. archive the repository after confirming there is no active branch or release that still needs edits;
3. do not present historical repositories as alternative installation paths;
4. keep old repository names only as redirect/history surfaces.

A repository rename from `Wist2` to `Wist` is a separate compatibility decision. Do not perform it as part of a promotion-only change because package links, documentation, GitHub Pages, clones, and external references must be checked together.

## Profile placement

Pin `Misha1302/Wist2` in the maintainer profile and place it before historical Wist repositories.

## Launch preflight

Before increasing promotion:

- run the source demo from a clean checkout;
- run `./Tools/smoke-published-wist-package.sh 0.1.0-alpha.1`;
- ask at least three independent .NET developers to reach the first numeric result;
- record exact friction points instead of asking only whether they liked the project;
- fix onboarding when a tester cannot reach the first result within ten minutes;
- confirm README, NuGet README, docs site, and GitHub About text use the same product sentence;
- confirm the social preview renders legibly at small size.

## Launch sequence

1. Publish a short maintainer post to existing professional contacts and .NET communities.
2. Incorporate installation and diagnostics feedback.
3. Publish the detailed .NET community/Reddit post.
4. Publish Show HN only when the package quickstart works without repository archaeology.
5. Submit to curated .NET lists after the public entry path and support expectations are stable.

Do not coordinate artificial votes, stars, or comments. Ask for technical feedback and real use cases; stars should be a by-product of usefulness and clear presentation.

## Metrics

Track:

- successful clean-room package runs;
- median time to first numeric result;
- external issues or discussions with actionable feedback;
- distinct real formulas attempted;
- external examples, pull requests, or integrations;
- README → NuGet/doc click-through when analytics are available;
- stars only as a secondary distribution signal.
