# External blind fault corpus kit

Status: `BLOCKED_EXTERNAL` until a human author outside the Wist2 result-producing process freezes a corpus.

This kit contains author instructions, public architecture information, prohibited disclosures, JSON templates, deterministic freeze tooling and a safe blind-import validator. It contains no authored fault corpus and therefore supports no independent-corpus claim by itself.

Workflow:

1. Give an external author only `HUMAN_FAULT_AUTHOR_PACKET.md`, `PUBLIC_ARCHITECTURE.md`, `PROHIBITED_INFORMATION.md` and `templates/`.
2. The author creates `faults/*.json` and `controls/*.json` without seeing policy results.
3. The author runs `freeze_corpus.py AUTHOR_DIRECTORY OUTPUT.tar.gz`.
4. The maintainer runs `validate_blind_import.py OUTPUT.tar.gz RECEIPT.json` before any policy execution.
5. Keep the archive checksum and import receipt immutable. Never mix these cases into historical primary/challenge denominators.
