# Historical semantic-bug corpus protocol

Protocol version: 1
Frozen before candidate discovery: 2026-08-03
Pre-study cutoff: 2026-07-29T23:59:59Z
Status: candidate discovery not yet executed when this protocol was written.

## Purpose

This corpus evaluates obligation-guided reverification on defects that existed before the CGO 2027 study. It is author-selected and author-backported; it is not an independent corpus.

## Candidate universe

Discovery considers all repository evidence available before the cutoff:

1. every GitHub issue created on or before the cutoff, including closed issues;
2. every commit authored or committed on or before the cutoff whose message or changed tests indicate a bug fix, regression repair, validation repair, incorrect result, late failure, ownership, capability, invalidation, stale state, or contract defect;
3. pre-cutoff regression tests whose names or surrounding commit explain an earlier semantic failure;
4. review notes or checked-in evidence documents created on or before the cutoff.

The search log records every query and pagination boundary. Candidate IDs are assigned before replay.

## Inclusion criteria

A candidate is included exactly when all conditions hold:

- the defect demonstrably existed before the cutoff;
- its symptom concerns a semantic contract, compiler-fact validity/invalidation, verifier ownership/routing, selected capability, source/producer identity, or a structurally legal artifact that reaches a wrong result or later failure;
- an oracle can be derived without consulting P2 output, from the original issue, failing test, reproducer, or fixing change;
- the defect can be reproduced on its original revision, or through a minimal documented backport that does not add the studied scheduler;
- the case can be represented without changing the original expected validity/result/diagnostic family.

## Exclusion criteria

Candidates are excluded, with a recorded reason, when they are only:

- syntax/parse rejection with no selected semantic relation;
- build, packaging, documentation, formatting, CI, or infrastructure failures;
- performance-only regressions;
- defects introduced after the cutoff;
- duplicates of a previously included root cause;
- unreproducible because required source/dependency evidence is unavailable;
- cases whose oracle can only be inferred from the new P2 behavior.

Unreproducible and invalid candidates remain in the accounting table; they are not silently dropped.

## Freeze and replay order

1. Enumerate candidates and freeze `candidates.json` plus its SHA-256.
2. Derive the oracle and mapping to modeled facts without running P0--P3/P1D.
3. Freeze any historical source/reproducer archive and its SHA-256.
4. Run all applicable policies on every included case.
5. Record misses, wrong results, late failures, invalid replays, and exclusions.
6. Generate aggregate tables only from the immutable per-case records.

## Backport constraint

A minimal backport may add only the harness needed to expose the historical symptom and adapt obsolete build APIs. It may not introduce obligation scheduling, new verifier logic, or an oracle derived from current policy output. Every changed line is listed in the case receipt.

## Required provenance fields

- candidate/case ID;
- issue/commit/test evidence and original date;
- original failing revision;
- original symptom and oracle source;
- fixing revision, when available;
- mapped fact, effect, route owner, and first eligible boundary;
- reproduction mode: exact historical revision or documented backport;
- policy result, first detection boundary, diagnostics, and result;
- inclusion/exclusion status and reason;
- all source/archive/record SHA-256 values.

## Claim boundary

The corpus can show that the mechanism detects or misses historically occurring defects selected under this protocol. It cannot establish independence, prevalence, fact/effect completeness, or whole-compiler correctness.
