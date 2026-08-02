# Anonymous CGO 2027 paper draft

Status: evidence-backed anonymous draft; not a submission receipt.

The source uses the current CGO 2027 ACM SIGPLAN `sigplan,screen,review,anonymous` format. The official limit is 11 pages of text excluding references; the current PDF contains five total pages. The paper deliberately uses the neutral name **System W** instead of the public repository/project identity.

Build without BibTeX:

```bash ci-run=false
cd CGO27/paper
pdflatex -interaction=nonstopmode -halt-on-error main.tex
pdflatex -interaction=nonstopmode -halt-on-error main.tex
pdflatex -interaction=nonstopmode -halt-on-error main.tex
```

The provider workflow rejects undefined citations/references, overfull horizontal boxes, non-Letter output, more than 11 total pages, unembedded fonts and identity/path leaks. The committed primary-source ledger contains 33 source records; the paper cites the 20 most load-bearing items.

The paper has no whole-compilation speedup claim. Pinned-machine performance and an externally authored blind corpus remain explicit blockers.
