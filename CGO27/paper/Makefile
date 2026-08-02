PDF=main.pdf

all: $(PDF)

$(PDF): main.tex references.tex sections/*.tex
	pdflatex -interaction=nonstopmode -halt-on-error main.tex
	pdflatex -interaction=nonstopmode -halt-on-error main.tex
	pdflatex -interaction=nonstopmode -halt-on-error main.tex

clean:
	rm -f main.aux main.log main.out main.pdf main.fdb_latexmk main.fls
