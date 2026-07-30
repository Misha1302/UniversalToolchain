# UniversalToolchain.ContractReviewHoldouts

Non-packable post-freeze holdout executable for the contract-guided reverification research line.

Run from the repository root:

```bash
bash ./Tools/run-contract-review-holdout.sh artifacts/contract-review-holdout
```

The executable writes raw JSONL, a bounded summary, environment metadata and a checksum-covered evidence tree. The cases were derived from a later adversarial review. They are intentionally reported separately from the original primary and challenge corpora and do not support an external-independence claim.
