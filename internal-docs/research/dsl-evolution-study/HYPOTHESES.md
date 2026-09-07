# Frozen hypotheses

Frozen before treatment implementation on 2026-09-08 against master `40117eb68c630f7129c120aaaadc69be8f4ecbfb`.

- **H1.** After recurring variability appears, UT-style explicit feature reuse reduces duplicated linguistic implementation relative to clone-and-own.
- **H2.** UT-style composition reduces change propagation / dispersion for a shared linguistic behavior change.
- **H3.** The first fixed variant is cheaper to implement without the complete composition ceremony.
- **H4.** A downstream compiler/IR improvement can be shared in both a well-structured shared-IR baseline and UT; this benefit is not evidence that source languages must be extensible.
- **H0.** On this workload UT composition has no observed benefit, or its extra ceremony/platform cost offsets the reuse benefit.

These hypotheses are not to be rewritten after measurements. Material workload changes require a dated amendment and separate commit.
