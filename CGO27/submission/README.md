# Anonymous review supplement

This directory defines the deterministic builder and acceptance checks for the double-blind review supplement.

The generated archive contains:

- an anonymized source snapshot with repository metadata removed;
- anonymous paper source and the provider-built PDF;
- raw and aggregate experiment evidence produced from the anonymized snapshot;
- content-addressed manifests and clean-unpack receipts;
- explicit `BLOCKED_EXTERNAL` and `BLOCKED_PINNED_MACHINE` claim-boundary receipts.

The review supplement intentionally does not contain an author name, public repository URL, public commit identifier, CI run identifier, or local absolute path. Source provenance is represented by a SHA-256 digest of the anonymized source archive.

The external human-authored corpus and pinned-machine whole-compilation study are not synthesized by this builder. Their blocked states are preserved rather than converted into unsupported claims.
