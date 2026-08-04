# Anonymous CGO 2027 supplementary package

This package is the double-blind companion to the paper **Obligation-Guided Reverification for Composed Compiler Pipelines**. It contains the raw bounded-study observations needed to recompute the cross-study ablation summary, the anonymous paper source, and the protocols that delimit every claim.

It intentionally does **not** contain the public repository URL, author identity, exact Git revision, package branding, local paths, or a whole-compilation performance result. System names are neutralized as **System W** and **System T**. The non-anonymous archival artifact remains separate and must not be uploaded during double-blind review.

Run the fast consistency and anonymity check:

```bash
bash quick-check.sh artifacts/quick
```

Run the full check, which also builds the paper when `pdflatex` is available:

```bash
bash reproduce.sh artifacts/full
```

Expected receipts:

```text
CGO27_ANONYMOUS_SUPPLEMENT_QUICK_CHECK=PASS
CGO27_ANONYMOUS_SUPPLEMENT_REPRODUCE=PASS
```
