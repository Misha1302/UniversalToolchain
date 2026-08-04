# Anonymous CGO 2027 supplementary package

This package is the double-blind companion to the paper **Obligation-Guided Reverification for Composed Compiler Pipelines**. It contains the raw bounded-study observations needed to recompute the cross-study ablation summary, the anonymous paper source, and the protocols that delimit every claim.

It intentionally does **not** contain the public repository URL, author identity, exact Git revision, package branding, local paths, or a whole-compilation performance result. System names are neutralized as **System W** and **System T**. The non-anonymous archival artifact remains separate and must not be uploaded during double-blind review.

Run the fast consistency and anonymity check after unpacking the supplementary archive:

```bash ci-run=false
bash quick-check.sh artifacts/quick
```

Run the full check after unpacking; it also builds the paper when `pdflatex` is available:

```bash ci-run=false
bash reproduce.sh artifacts/full
```

These commands are package-local and therefore are not executed by the repository-root Markdown smoke runner. The provider artifact workflow performs the equivalent clean-unpack checks.

Expected receipts:

```text
CGO27_ANONYMOUS_SUPPLEMENT_QUICK_CHECK=PASS
CGO27_ANONYMOUS_SUPPLEMENT_REPRODUCE=PASS
```
