---
title: Runtime Profiles
description: Retirement note for the former runtime-profile compatibility API.
---

# Runtime Profiles

Status: **retired in S13**.

The former `RuntimeProfileDefinition` / runtime-profile host API belonged to the reflection-based dialect integration topology and has been physically removed. New language/runtime configuration must be represented in typed `LanguageDefinition` data, feature/contribution descriptors and `LanguageRuntimePolicy`, then compiled exactly once by `LanguageCompiler` into `LanguagePlan`.

Do not recreate runtime-profile overlays after planning: `LanguageRuntime` materializes the already selected plan and must not choose a second feature, backend or implementation graph.

Runtime-manifest emission metadata is a separate tooling/package concern and is not a replacement for typed planning.
