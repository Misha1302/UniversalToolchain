# Public architecture supplied to the external author

The evaluated compiler is an extensible pipeline with semantic boundaries:

`source -> parser -> AST -> bytecode -> AIR -> optimization -> backend -> execution`.

Extensions may declare facts, capabilities, effects and verifier ownership. An optimization may preserve, invalidate or require reverification of semantic facts. Candidate fault families include stale facts, incorrect shape/type/layout assumptions, missing capability selection, ownership conflicts, invalid lowering relations, wrong-result transformations and failures detected only by a later backend or runtime.

The author should specify expected behavior and the first boundary at which a correct verifier could detect the fault. The author is not given policy-specific detection results or existing corpus answers.
