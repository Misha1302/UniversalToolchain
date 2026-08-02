# Anonymous CGO 2027 paper draft

Status: strengthened evidence-backed anonymous draft; not a submission receipt.

The source uses the ACM SIGPLAN `sigplan,screen,review,anonymous` format. The paper uses the neutral names **System W** and **TensorRules** rather than public repository identity. The scientific center is the relative boundary-safety property; P2/P3 parity and verifier-call counts are supporting results rather than the headline claim.

Build without BibTeX:

```bash
cd CGO27/paper
pdflatex -interaction=nonstopmode -halt-on-error main.tex
pdflatex -interaction=nonstopmode -halt-on-error main.tex
pdflatex -interaction=nonstopmode -halt-on-error main.tex
```

The provider workflow rejects undefined citations/references, overfull horizontal boxes, non-Letter output, more than 11 pages, unembedded fonts, and identity/path leaks. The paper has no whole-compilation speedup or independent-corpus claim. The demand-driven baseline is a separate controlled witness and does not alter frozen P0--P3 denominators.
