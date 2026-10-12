# Protocol amendment A2: documentation parity for the exact test census

**Chronology:** after A1's observed passing 504 Core tests and the count-manifest update; before changing documentation.

Source-grounded audit of `Tools/check_documentation_status.py` at master shows two independent checks: it requires a row per `eng/test-counts.json` entry and exact total in both `VERIFICATION.md` and `docs/evidence/current-verification.md`; it also rejects stale active numeric totals in `RELEASE_NOTES_RU.md` and related current docs. The branch's canonical CI must not be made green by disabling this contract.

**Scope expansion:** restrict modifications to three exact existing documentation files: `VERIFICATION.md`, `docs/evidence/current-verification.md`, `RELEASE_NOTES_RU.md`. In their active test-count rows/statement only, update **Core 500→504** and **total 1,324→1,328**. No historical workflow SHA, original paper evidence, release package identities, candidate publication claims, or the older 2026-08-18 factual snapshot are changed. A current contract count is distinguished from historical execution receipts.

**Why necessary:** original pre-registration allowed only a new test and research documentation. The independent existing CI rejected the unsynchronized new count. A1 repaired the machine-readable contract; this A2 repairs required human-readable projections. The initial pre-registration remains unchanged and continues to report its narrower original mutation scope.

**Independent validation requirement:** `python3 Tools/check_documentation_status.py`, normal GitHub Docs Check plus full exact-count CI. A failure after these changes must be investigated, not bypassed.
