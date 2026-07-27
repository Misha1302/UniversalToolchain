# Матрица parity Wist

Состояние на 26 июля 2026 года: **typed runtime является канонической реализацией Wist**.

- Все production runtime exports покрыты typed package.
- Все 7 оптимизаторов представлены typed contributions.
- Канонические backend IDs: `cil` и `interpreter`; alias `compiler` удалён.
- Все 7 shipped presets создаются через `WistLanguageDefinitions` и входят в исполняемый regression-gate.
- Compatibility runtime-pack API, текстовый adapter и старые schema readers удалены.

Машиночитаемый источник истины: `WIST_PARITY_MATRIX.json`.
