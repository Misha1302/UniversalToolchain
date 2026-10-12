# Protocol amendment A3: live release-readiness test-count projection

After A2, [GitHub Docs Check run 38246661346](https://github.com/Misha1302/UniversalToolchain/actions/runs/38246661346) passed `Tools/check_documentation_status.py` and release-state validation, but `Tools/check-wist-release-readiness-docs.py` rejected `docs/evidence/wist-stability-v0.1.0-alpha.7.md`: its line declared **1,324** current tests against the research branch's **1,328** test manifest. The checker explicitly binds that document's count to `eng/test-counts.json`, regardless of the older candidate version in the filename.

**Narrow mutation extension before edit:** modify only the active exact test-count sentence in that stability file to `1,328`, and clarify that this number reflects current branch verification, not a claim about historical Wist release readiness. Do not touch published package metadata, baseline receipts, code or any validation logic. The preregistration and A1/A2 remain intact; this is a new independently discovered documentation owner dependency.

Afterwards rerun the same Docs Check, canonical CI and release-readiness validator. A green status on an older commit does not validate the new head.
