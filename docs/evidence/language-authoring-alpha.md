---
title: Language Authoring Alpha Status
description: Implemented generic framework contracts and remaining product gaps.
---

# Language Authoring alpha status

## Implemented and covered

- typed artifact contracts and exact runtime type checks;
- package/feature/contribution model;
- dependency, conflict, capability and slot planning;
- explicit slot replacement and preferred capability provider;
- configurable entry artifact;
- deterministic conversion-route and same-artifact pass planning;
- schema-v6 canonical lock representation and plan hash (package manifests remain schema v5);
- exact package manifest binding during runtime assembly;
- exact backend executor selection;
- per-session and explicit stateless singleton lifetimes;
- synchronous/asynchronous disposal with in-flight-operation coordination;
- fail-closed determinism and host-interop policy validation;
- independent non-Wist sample, template and package-consumer smoke;
- Language SDK coverage tracked by the revision-bound `eng/test-counts.json` and `VERIFICATION.md` contracts.

## Alpha and expected to change

- builder vocabulary and package boundaries;
- diagnostic detail and convenience APIs;
- lock schema after explicit migration/versioning;
- third-party packaging ergonomics;
- cross-package version-resolution policy beyond exact selected identities;
- higher-level testing helpers.

## Not implemented as a product promise

- grammar/parser generation;
- high-level binder/type-system DSL;
- IDE/editor workbench;
- arbitrary plugin trust or in-process sandboxing;
- stable 1.0 generic API;
- proof that every future backend requires no framework evolution.
