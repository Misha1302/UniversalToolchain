# UniversalToolchain → Ideal UT: единый инвентарь развития

> **Базовая ревизия:** [`002a71f0fcdabe7915ed139b297b3829b8060dc2`](https://github.com/Misha1302/UniversalToolchain/commit/002a71f0fcdabe7915ed139b297b3829b8060dc2), `master`, 8 октября 2026 года.\
> **Дата оформления инвентаря:** 9 октября 2026 года.\
> **Назначение:** полный перечень направлений, задач, экспериментов и изменений для последующей приоритизации; **не roadmap, не задание на немедленное внедрение и не перечень утверждённых обязательств**.\
> **Основа:** предшествующий аудит HEAD, все 14 файлов `internal-docs/research/ideal-ut-2026-10-08/`, текущая документация, выборочная проверка исходников/тестов/примеров, релевантные issues/PR. Новая сборка, повторный прогон тестов и повторная проверка HEAD при изготовлении этого Markdown-файла **не выполнялись**.

## Как читать реестр

- **P0** — фундаментальное требование или критическая предпосылка *соответствующей заявленной возможности*. P0 **не означает**, что вся гипотетическая подсистема утверждена к реализации.
- **P1** — важный путь развития. **P2** — полезное при спросе/измеримом эффекте. **P3** — отдалённое исследование или возможность.
- **NEW** — целевой функционал не подтверждён в `master`; **PARTIAL** — уже есть существенная часть; **EXPERIMENTAL** — исследовательский прототип, отдельная кандидатная реализация либо гипотеза; **DONE** — реализация подтверждена в заявленном объёме; **UNKNOWN** — недостаточно проверено.
- Коды `[I]`, `[E]` и т. п. — указатели на **конкретные исходные файлы**, расшифрованные в разделе «Источники». Их наличие обосновывает включение пункта, **но не подтверждает его реализацию**.
- Отдельно держать три плоскости: **текущее продуктово-инженерное состояние**, **исследовательские предложения** и **наши оценочные приоритеты**. Нельзя выводить одно из другого.
- Повторяющиеся базовые понятия в разных разделах — не призыв создавать дубликаты API. Например, `ProgramSnapshot` должен иметь одного владельца, а IDE, MCP и Web UI — лишь использовать его проекции.

## I. Основной реестр: 15 направлений

### 1. Core Architecture & Composition

**Смысл:** превратить уже работающую композицию *реализаций* в безопасное взаимодействие независимых семантических возможностей, не нарушив единственного владельца глобальных решений — `LanguageCompiler`. Базовые типизированные пакеты, маршруты и runtime **уже существуют**.

- `[P0][PARTIAL]` **Semantic Composition** — научить независимые модули обмениваться проверяемыми свойствами; не путать наличие capability с доказанностью её поведения. `[I, E]`
- `[P0][PARTIAL]` **N+1 Extensibility** — новый provider должен улучшать неизменённый consumer без перекомпоновки исходников старых пакетов; проверять на внешних сборках. `[B, E]`
- `[P0][NEW]` **ProgramSnapshot** — ввести неизменяемую идентичность ревизии программы, phase/IR и anchors; исключить использование фактов от прежнего артефакта. `[I, K]`
- `[P0][PARTIAL]` **Local Feasibility** — после выбора конфигурации проверять допустимость действий для конкретной программы; не превращать локальную проверку в новый глобальный planner. `[B, E]`
- `[P0][PARTIAL]` **Executable Identity** — привязать факты к фактическим сборкам/implementation digests; один `PlanHash` идентифицирует данные плана, а не весь исполняемый код. `[I, K]`
- `[P1][PARTIAL]` **Semantic Compatibility** — отдельно проверять соединяемость typed contracts и реальную совместимость поведения; структурный маршрут не является сертификатом эквивалентности. `[C, E]`
- `[P1][PARTIAL]` **Provider Evolution** — обеспечить замену реализации с управляемым пересчётом её зависимых фактов; несовместимый апгрейд должен быть виден. `[I, W]`
- `[P1][PARTIAL]` **Cross-Package Interop** — уменьшать ручные адаптеры между независимыми авторами, но не скрывать обязательные границы сериализации/типов. `[E, G]`
- `[P1][PARTIAL]` **Planning Explainability** — показывать, почему выбран provider/route или возник отказ, как проекцию текущего `LanguagePlan`, без независимого explain-планировщика. `[F, D]`
- `[P1][PARTIAL]` **Component Concurrency** — документировать разрешённую параллельность runtime-компонентов; lifetime gate не гарантирует reentrancy каждого extension. `[G, F]`
- `[P2][NEW]` **Persistent Plan Cache** — кешировать одинаковые разрешения конфигураций только при измеримом bottleneck; ключ обязан учитывать canonicalization и версии. `[F]`
- `[P2][NEW]` **Incremental Planning** — перепланировать только затронутые зависимости при доказанном выигрыше для крупных workspace; не создавать скрытых stale states. `[F]`
- `[P2][NEW]` **Route Search Indexing** — исследовать индексирование графов на сотнях/тысячах contributions, сохраняя детерминированную диагностику и отказ при неоднозначности. `[F]`
- `[P3][PARTIAL]` **Advanced Version Resolution** — расширять существующую точную идентификацию до диапазонов версий лишь при конфликтующих независимых пакетах; не дублировать NuGet. `[F, W]`

**Архитектурная граница:** `LanguageDefinition → LanguageCompiler → immutable LanguagePlan → LanguageRuntime` сохраняется. Runtime проверяет и материализует уже выбранные компоненты; поведение произвольных plugins не становится «доказанным» от валидного плана.

### 2. Semantic System & Evidence

**Смысл:** дать независимым компонентам право задавать и проверять узкие семантические вопросы, не превращая непроверенные декларации в разрешение опасных оптимизаций. Это **research proposal**, а не готовый стабильный generic fact service. Начальная альтернатива — обычные typed C# interfaces.

- `[P0][EXPERIMENTAL]` **Typed Semantic Properties** — типизированные предикаты и значения вместо совпадения строковых тегов; описывать домен и владельца смысла. `[V, K]`
- `[P0][NEW]` **Versioned Predicate Schemas** — устойчивые схемы/версии и правила совместимости; одинаковое имя без одинаковой семантики не означает совместимости. `[K, B]`
- `[P0][NEW]` **Semantic Subjects** — точные объекты запроса: операция, значение, регион, program point и path; запретить неоднозначные глобальные утверждения. `[I]`
- `[P0][EXPERIMENTAL]` **Four-Valued Evidence** — `Proven`, `Disproven`, `Unknown`, `Contradiction`; отсутствие доказательства никогда не преобразовать в `false` либо `safe`. `[V, E]`
- `[P0][NEW]` **Evidence Authority** — различать заявление автора, верифицированное наблюдение и формальный вывод; декларация `Pure` сама себя не подтверждает. `[I, V]`
- `[P0][NEW]` **Trust Policies** — доверять только явно разрешённым источникам/верификаторам для correctness-critical решений; политика — часть контекста запроса. `[K, E]`
- `[P0][NEW]` **Scoped Evidence** — ограничить свидетельство backend, фазой, программой, путём, assumptions и selected executable set; не переиспользовать шире области истинности. `[I, K]`
- `[P0][NEW]` **Proof Provenance** — сохранять premises, владельца, версию бинарника, правило вывода, verifier и ревизию для объяснения и отзыва. `[I, V]`
- `[P0][NEW]` **Evidence Invalidation** — отзывать транзитивную зависимую цепочку после изменения IR, provider, backend, trust policy или исходной программы. `[B, E]`
- `[P0][NEW]` **Contradiction Handling** — явное конфликтное состояние для двух несовместимых доверенных фактов, без last-writer-wins и случайного приоритета. `[I, V]`
- `[P0][NEW]` **Open-World Semantics** — неполные сведения → `Unknown`; избегать negation-as-failure на открытом наборе расширений. `[V]`
- `[P0][NEW]` **Dependency Completeness** — утверждение «для всех зависимостей» требует доказанной полноты перечисления; иначе заключение недопустимо. `[I, E]`
- `[P1][EXPERIMENTAL]` **Semantic Query API** — одинаковый интерфейс у независимых providers/consumers; запрос не должен выбирать новые packages вне плана. `[K, B]`
- `[P1][EXPERIMENTAL]` **Semantic Vocabulary** — минимальные стабильные отношения `DependsOn`, `Captures`, `Serializable`, `Pure`; расширять только при независимом использовании. `[V]`
- `[P1][PARTIAL]` **Algebraic Laws** — свойства ассоциативности, коммутативности и т. п. привязывать к домену/FP/overflow, а не к произвольному имени операции. `[M, V]`
- `[P1][PARTIAL]` **Effect Semantics** — развить текущие summaries до памяти, aliasing, exceptions, ordering, mutable state; консервативное поведение при неизвестном. `[M, I]`
- `[P1][NEW]` **Proof Obligations** — предусловия каждого преобразования в проверяемой форме; profitability считать лишь после legality. `[I, K]`
- `[P1][NEW]` **Preservation Certificates** — проверять `preserves`/`invalidates` по независимой политике, а не считать утверждение pass сертификатом. `[K, E]`
- `[P1][NEW]` **Delegates/Captures Semantics** — показать независимое объединение capture graph, сериализуемости, устойчивости identity и backend feasibility. `[V]`
- `[P3][EXPERIMENTAL]` **Bounded Inference Kernel** — исследовать ограниченные Horn/Datalog правила только при двух реальных транзитивных потребностях и ясной окупаемости. `[D, V]`

**Главный эксперимент:** независимые `ShapeAnalysis` и `RangeAnalysis` снабжают уже скомпилированную `BoundsOptimization` достаточными основаниями, чтобы удалить *реальную* проверку границ; при любом stale/negative/contradictory/unknown evidence оптимизация обязана оставить check.

### 3. IR, SSA & Optimizations

**Смысл:** расширить проверяемость и выразительность реальных Bytecode/AIR/SSA путей, не превращать SSA в обязательную структуру каждого языка. Уже есть verifier-gated SSA alpha, частичный callable lowering и несколько оптимизаторов.

- `[P0][PARTIAL]` **Bytecode Verification** — полнее проверять корректность тегов, типов, стековых эффектов и соглашений перед AIR и backend. `[X, O]`
- `[P0][PARTIAL]` **Bytecode Tag Registry** — явно связать typed tags с producers, consumers, владельцами и конфликтами; устранить magic-string coupling. `[X, O]`
- `[P0][PARTIAL]` **Declarative Bytecode Operations** — заменить часть behavior-bearing callbacks на данные + typed handlers; эксперимент существует лишь в открытом PR #371. `[PR371]`
- `[P0][EXPERIMENTAL]` **Plan-Owned Operation Handlers** — привязать обработчик операции к выбранному provider/`LanguagePlan`; текущий PR #371 использует внутренний static registry. `[PR371]`
- `[P0][PARTIAL]` **Typed AIR Verification** — усилить CFG, stack merge, типы, backend requirements и effects; не предполагать полноту уже существующих проверок. `[X, S]`
- `[P0][NEW]` **Guarded Transformations** — применять преобразования только при доверенных выполненных obligations; отказ должен сохранять исходный эквивалентный путь. `[I, K]`
- `[P0][NEW]` **Transformation Observation Model** — фиксировать семантические наблюдения: значения, traps, heap, ordering, exceptions, overflow и FP. `[I]`
- `[P0][NEW]` **Cross-IR Fact Transfer** — удостоверять one-to-many AST↔AIR↔SSA mappings, иначе повторять анализ либо возвращать `Unknown`. `[E, K]`
- `[P1][PARTIAL]` **Callable-First SSA** — развивать descriptor-driven operations и neutral core, не возвращать hardcoded arithmetic universe. `[M, S]`
- `[P1][PARTIAL]` **AIR → SSA Coverage** — добавить unsupported intrinsics, execution-scoped provider shapes, value types и control-flow случаи с корректными отказами. `[S]`
- `[P1][PARTIAL]` **SSA → AIR Scheduling** — обеспечить repeated-value use, spills, duplication, stack rearrangement и проверяемую обратную эмиссию. `[S]`
- `[P1][PARTIAL]` **SSA Return Model** — определить функцию/терминаторы с несколькими возвращаемыми значениями и явный законный маршрут обратно в AIR. `[S]`
- `[P1][PARTIAL]` **Managed Callable Mapping** — расширить CLR mappings, instance/constructor/generic shapes и сохранить точные execution-scoped bindings. `[M, S]`
- `[P1][PARTIAL]` **Constant Representation** — обобщить константы поверх нынешнего int32/bool-compatible alpha и обеспечить типовую точность. `[M]`
- `[P1][PARTIAL]` **SSA Optimization Suite** — расширять cross-block GVN, LICM, inlining и анализы только с корректностными оракулами. `[S]`
- `[P1][PARTIAL]` **SSA CSE Scheduling** — снять ограничения preview-маршрута после поддержки повторного использования SSA значения при AIR эмиссии. `[S]`
- `[P1][NEW]` **Declarative Rewrite Rules** — ограниченные правила с проверяемыми законами, effects и условиями; не общая небезопасная pattern магия. `[D, E]`
- `[P1][NEW]` **Correctness-Gated SIMD** — только после доказательств bounds, alias, lane/tail, exceptions и поддержки target; LangDev SIMD — концепция. `[I, E]`
- `[P2][PARTIAL]` **IR Policy Syntax** — оформить явные `off/prefer/require/debug` политики на уровне dialect/authoring; сохранить старый alias как совместимость. `[M]`
- `[P2][NEW]` **SSA-Native Backend** — исследовать исполнение из SSA без обязательного roundtrip в AIR; отдельный backend с отдельной parity-обязанностью. `[S]`
- `[P2][NEW]` **IR Region Feasibility** — bounded local choices по областям конкретной программы без изменения набора пакетов или добавления второго planner. `[B, E]`
- `[P3][NEW]` **E-Graph Optimization** — equality saturation лишь для узких подходящих workloads и законов с проверенной областью применимости. `[J, D]`

**Особая граница:** legality ≠ profitability. Не разрешать оптимизацию только потому, что она выгодна; не считать круговую проверку interpreter/CIL независимым математическим доказательством.

### 4. Language Authoring & Language SDK

**Смысл:** перейти от существующего low-level SDK, который *композирует написанные вручную реализации*, к удобному созданию самостоятельных языков. Ни grammar generation, ни generic binder DSL не следует приписывать текущему alpha.

- `[P0][NEW]` **Grammar SDK** — декларативные production/precedence/associativity definitions для внешних языков без Wist-зависимостей. `[L, P]`
- `[P0][NEW]` **Parser Generation** — генерировать/собирать parser по грамматике с source spans и диагностикой конфликтов. `[L, G]`
- `[P0][NEW]` **Binder SDK** — декларативная и программная привязка имён, областей видимости и ссылок на symbol identities. `[L, P]`
- `[P0][NEW]` **Type System SDK** — авторство пользовательских типов, разрешения выражений и совместимости на независимом DSL. `[L, K]`
- `[P1][NEW]` **Operation Definition SDK** — одна typed declaration для signatures, effects и допустимого lowering с ручным escape hatch. `[L, K]`
- `[P1][PARTIAL]` **Module Contracts** — миграция token/visitor/priority/bytecode соглашений к явным machine-checkable descriptors. `[O]`
- `[P1][NEW]` **Grammar Conflict Analysis** — обнаруживать overlap и приоритетные конфликты не только по тестовым примерам. `[O]`
- `[P1][PARTIAL]` **Canonical Semantic Nodes** — заменить Wist legacy projections, сохранив immutable semantic boundary между syntax и lowering. `[C]`
- `[P1][NEW]` **Syntax Preservation** — lossless spans, comments/trivia, line endings для корректного форматирования и редакторских правок. `[P]`
- `[P1][PARTIAL]` **Language Templates** — развить имеющийся `dotnet new ut-language`, не выдавая его за готовый high-level workbench. `[G, P]`
- `[P1][NEW]` **Language Family Templates** — отдельные правила/политики/конфигурационные DSL как примеры реальной разработки. `[B, P]`
- `[P1][NEW]` **Host Symbol Binding** — связывать DSL symbols с типизированной .NET host schema и явно ограниченными разрешениями. `[P]`
- `[P1][NEW]` **Authoring Facade** — fluent `RuleLanguage`-подобный слой поверх текущих builders, без альтернативного parser/compiler/runtime truth. `[K]`
- `[P2][NEW]` **Source Generators** — генерировать metadata и registrations только если меньше ошибок/LOC и есть спрос AOT/trimming. `[D, F]`
- `[P2][NEW]` **Generated Contract Tests** — выводить типичные позитивные/негативные сценарии из деклараций операций, не подменяя независимый oracle. `[O]`
- `[P2][NEW]` **Cross-Language Composition Examples** — два несовпадающих по семантике языка, проверяющих отсутствие Wist-hardcode в generic SDK. `[B, E]`

**Ключевой нюанс:** синтаксическое комбинирование и семантическая композиция — разные задачи. Отдельное authoring-упрощение можно продвигать независимо от недоказанной evidence ontology.

### 5. IDE, LSP & Incremental Language Services

**Смысл:** предоставить IDE функции поверх *тех же* binder/types/symbol semantics, что используются batch-компилятором. Локальный редактор обязан переживать некорректный/незавершённый текст и гонки асинхронных запросов.

- `[P0][NEW]` **DocumentSnapshot** — immutable версия исходного текста, spans и семантических результатов для редактора; устаревшие ответы не могут менять новое состояние. `[P]`
- `[P0][NEW]` **Error-Tolerant Parsing** — partial AST/holes для незавершённого редактирования; `Unknown` вместо ложной уверенности. `[P]`
- `[P1][NEW]` **Incremental Analysis** — переанализировать изменившиеся fragments после документно-версионированного invalidation; измерить выигрыш над full analysis. `[P, E]`
- `[P1][NEW]` **LSP Server** — адаптер стандартного протокола к общей language-services модели, а не второй независимый binder. `[P]`
- `[P1][NEW]` **Text Synchronization** — open/change, cancellation и document versions; valid→invalid→valid с отбрасыванием stale callbacks. `[P, B]`
- `[P1][PARTIAL]` **Live Diagnostics** — выводить имеющиеся typed/compiler diagnostics с точными spans в редакторе; нормализовать инкрементальные ошибки. `[C, P]`
- `[P1][NEW]` **Completion** — подсказки по текущему контексту, видимым symbols и ожидаемым semantic types. `[P]`
- `[P1][NEW]` **Hover** — краткие сведения о типе, declaration, signature и контекстно допустимых свойствах. `[P]`
- `[P1][NEW]` **Go to Definition** — устойчивый символ и source anchor вместо поиска текстового совпадения. `[P]`
- `[P2][NEW]` **References/Rename** — безопасный межфайловый поиск и правки идентификаторов на выбранной версии документа. `[P]`
- `[P2][NEW]` **Editor Presentation** — semantic highlighting, formatting, signature help и inlay hints из тех же данных. `[P]`
- `[P2][NEW]` **Advanced Language Actions** — code actions и workspace symbols при подтверждённой необходимости multi-file editing. `[P]`

**Допущение:** использовать готовую LSP transport library, не разрабатывать протокол самостоятельно. Отдельная workspace index может появиться позднее; она не должна стать альтернативным компилятором.

### 6. Debugging, Diagnostics, Tracing & Visualization

**Смысл:** три разных наблюдаемых слоя — DSL end-user debugger, trace для разработчика компилятора и объяснение legality/provenance оптимизаций. Их визуализации не должны смешиваться с движком доказательств.

- `[P0][PARTIAL]` **Debug Trace v2** — дополнить уже имеющуюся `wistc run --trace` JSON-трассу полноценными стадиями без возврата `logs.txt`. `[T]`
- `[P1][NEW]` **Stage Artifacts** — структурированные immutable syntax/semantic/Bytecode/AIR/SSA dumps с версиями schemas и политикой приватности. `[T]`
- `[P1][PARTIAL]` **Failure Attribution** — фиксировать точный owner, phase, pass и diagnostic для исходной причины отказа. `[T, C]`
- `[P1][NEW]` **Partial Failure Trace** — корректно сбрасывать частичную трассу при ошибке компиляции/исполнения. `[T]`
- `[P1][NEW]` **Phase Timings** — замеры syntax/binding/lowering/optimization/backend без смешения performance категорий. `[T]`
- `[P1][NEW]` **Trace Viewer** — визуализировать реальные schema-versioned trace files, не образцы старых logs. `[T]`
- `[P1][NEW]` **IR/DAG Viewer** — графы blocks/values/dependencies, выбранных passes и точек преобразований. `[P, T]`
- `[P1][NEW]` **Proof Explorer** — показывать дерево предпосылок, provenance, failed obligations, reason-for-invalidation и отсутствие доказательств. `[P, I]`
- `[P1][NEW]` **Interpreter Debug Hooks** — events operation boundary, read/write, frames, calls и exceptions при сохранении исходной семантики. `[P]`
- `[P1][NEW]` **DAP Adapter** — source breakpoints, step/continue и отладочные представления поверх interpreter hooks. `[P]`
- `[P2][NEW]` **CIL Debug Mapping** — backend-generated sequence maps и честная деградация при inline/optimized-away locals. `[P]`
- `[P2][NEW]` **IR Diff Inspector** — before/after с семантическими obligations и объяснением, почему конкретный pass применён. `[P, D]`

**Приватность:** trace уже скрывает исходный текст/значения по умолчанию, однако сообщения исключений могут содержать секреты. Viewer должен соблюдать redaction policy, а не усиливать сбор данных автоматически.

### 7. MCP & AI-Agent Tooling

**Смысл:** сделать существующие типизированные compiler services доступными AI-агентам как read/propose/validate интерфейсы. Транспорт MCP не является независимым источником истины и не уполномочен менять выбранную конфигурацию.

- `[P1][NEW]` **MCP Adapter** — тонкий внешний транспорт к typed UT services; server не должен владеть planner, binder или fact store. `[K, D]`
- `[P1][NEW]` **Tool Schema Contracts** — версии API, request/response types, cancellation, diagnostic codes и точная область разрешённого доступа. `[K]`
- `[P1][NEW]` **QuerySymbols** — запрашивать symbol identity, signatures, types и spans без сканирования произвольных текстовых дампов. `[K]`
- `[P1][NEW]` **QuerySemanticFacts** — получать факты с provenance/trust/scope; возвращать `Unknown`, когда доказательств нет. `[K, I]`
- `[P1][NEW]` **ExplainDiagnostic** — выдавать причинную структуру ошибки и допустимые действия, не придумывая её из LLM output. `[K]`
- `[P1][NEW]` **ExplainPlan** — описывать выбранные package/contribution/route/provider прямо из `LanguagePlan`. `[D]`
- `[P1][NEW]` **WhyNot** — давать детерминированные reasons для отвергнутых choices и transforms, сохраняя неполноту знаний. `[D, P]`
- `[P1][NEW]` **PreviewEdit** — возвращать source patch/diff и diagnostics на точной версии без мутации текущего world. `[K, P]`
- `[P1][NEW]` **ValidatePatch** — проверять proposed patch реальными compiler services и test/contract policies. `[K]`
- `[P1][NEW]` **ApplyApprovedEdit** — применять изменения после явного разрешения и exact revision precondition, атомарно и с возможностью отката. `[K, P]`
- `[P1][NEW]` **Agent Trust Boundaries** — LLM не может само объявлять `Proven`, выбирать неподписанный package или разрешать unsafe rewrite. `[K, I]`
- `[P2][NEW]` **Read-Only Agent Inspection** — безопасно открыть plan, diagnostics и traces по allowlist инструментов и принципу least privilege. `[D]`
- `[P2][NEW]` **Agent Refactoring Evaluation** — сравнивать против plain assistant точность изменений, число регрессий, время review и воспроизводимость. `[P]`
- `[P3][EXPERIMENTAL]` **Independent Agent Oracles** — использовать AI для поиска контрпримеров только с независимой проверкой, не принимать текст AI за формальное доказательство. `[E, J]`

**Security invariant:** генерированный JSON/schema-valid вызов сам по себе не даёт полномочия на выполнение; side effects управляются host policy и подтверждённой версией снапшота.

### 8. CLI, Web UI & User Workflows

**Смысл:** поддержать внешние сценарии CLI и браузерного редактирования, не создавать отдельную mutable semantic model для каждой пользовательской оболочки. `wistc` уже работает, обобщённая UT CLI — другое направление.

- `[P1][PARTIAL]` **Wist CLI** — развивать существующий `wistc` и его canonical backend ids; не переписывать поддерживаемый интерфейс без migration. `[Q]`
- `[P1][NEW]` **Generic UT CLI** — независимые от Wist команды для определения, проверки и диагностики сторонних языков и пакетов. `[G, P]`
- `[P1][PARTIAL]` **InspectPlan** — выводить selected features, provider, artifact routes, exact identities и plan hash в удобном формате. `[C, D]`
- `[P1][NEW]` **ExplainFailure CLI** — причинно описывать отсутствующие capabilities, conflicts, неоднозначные routes и почему план не реализуем. `[D, F]`
- `[P1][PARTIAL]` **Trace CLI** — чтение, фильтрация и сравнение текущих структурированных trace artifacts с сохранением versions. `[Q, T]`
- `[P1][NEW]` **JSON Diagnostics CLI** — стабильная сериализация structured diagnostics для автоматизации/редакторов. `[P, K]`
- `[P1][NEW]` **Language Validation CLI** — единые проверки grammar, binding, declared contracts, backend support и identity. `[P, O]`
- `[P1][NEW]` **Browser DSL Editor** — текстовый editor, использующий общий binder и document snapshot через адаптер. `[P]`
- `[P1][NEW]` **Preview Playground** — выполнять/валидировать DSL на замороженных входах до approval; никакого неявного изменения активных правил. `[P]`
- `[P1][NEW]` **Semantic Diff UI** — сравнивать версии правил и диагностические последствия; не выдавать приблизительный diff за формальное доказательство. `[P]`
- `[P2][NEW]` **Forms/Table Editing** — представления формы/таблицы как преобразования Git-friendly текстового источника с проверкой версии. `[P]`
- `[P2][NEW]` **Graph Editing** — опциональные graph projections без обязательного projectional source of truth. `[P]`
- `[P2][NEW]` **Optimization Explorer** — показать feasibility, cost и фактический выбранный результат, не дублируя optimizer/planner. `[P, D]`

**Практический вертикальный сценарий:** открыть versioned policy → получить ошибки до запуска → preview на фиксированном документе → увидеть diff → отдельно approve → выполнить и сохранить audit identifiers.

### 9. Execution, Backends & Performance

**Смысл:** доказать, что гибкая платформа не разрушает быстроту конкретного исполнения. Уже есть interpreter и CIL; нельзя смешивать создание `WistEngine`, компиляцию формулы и вызов подготовленного делегата в одну метрику.

- `[P0][PARTIAL]` **Backend Contract Coverage** — третья независимая production-scale реализация проверит backend-neutral assumptions и упаковку API. `[L]`
- `[P0][PARTIAL]` **Interpreter/CIL Parity** — проверять значения, типы, errors, heap/effects и unsupported shapes; базовые parity tests уже существуют. `[C, E]`
- `[P1][PARTIAL]` **Callable Lowering Targets** — добавить реальные CIL/interpreter target emissions для предусмотренных SSA callable options. `[M]`
- `[P1][PARTIAL]` **Backend Capability Enforcement** — продолжить полную проверку intrinsics, operand semantics и эффекта каждого lowering target. `[M, X]`
- `[P1][NEW]` **Selected-Route Specialization** — специализация после доказательства допустимости по frozen selected route; не закладывать hidden dynamic rediscovery. `[E, B]`
- `[P1][NEW]` **Hot-Path Binding** — исключить per-call metadata/registry/fact lookups из подготовленного выполнения там, где это реально измерено. `[B]`
- `[P1][PARTIAL]` **Compilation Cost Modeling** — раздельно измерять planning, materialization, compile, first invocation и steady-state. `[R, E]`
- `[P1][PARTIAL]` **Performance Workloads** — расширить текущие benchmarks функциями, ветвлениями, аллокациями и крупными языковыми конфигурациями. `[R]`
- `[P1][NEW]` **Allocation Profiling** — профиль памяти snapshots, evidence, planner, compilation и hot path с фиксированным baseline. `[E]`
- `[P1][NEW]` **Invalidation Cost Benchmark** — измерить стоимость отмены/stale facts и recomputation после изменения providers/IR. `[E]`
- `[P2][NEW]` **Advanced Optimizing Backend** — экспериментальный `optimized-cil` без замены текущего CIL; Flame proposal отдельно требует лицензионного решения. `[Y]`
- `[P2][NEW]` **NativeAOT/Trimming Experiments** — проверять конкретные publish targets и используемую reflection/registration-модель, а не обещать универсальную поддержку. `[F]`
- `[P2][NEW]` **Configuration Churn Benchmarks** — стоимость частой смены packages/definitions и амортизации компиляции относительно статического pipeline. `[E]`
- `[P2][NEW]` **Bounded Execution Isolation** — проверить overhead и угрозы реального process worker, если запуск недоверенных программ станет поддерживаемым. `[U, F]`
- `[P3][UNKNOWN]` **Automatic Execution Tiering** — исследовать адаптивный выбор пути лишь после подтверждения профилей workloads и явной политики fallback. `[Y]`

**Экспериментальный контроль:** сравнивать с handwritten typed/static implementation на *той же* программе и с одинаковой границей вызова; «zero allocation» допускается только как частный вывод из конкретного benchmark.

### 10. Testing, Verification & Correctness

**Смысл:** проверять не только успешный результат сборки, но и то, что выбранная оптимизация/маршрут не нарушили семантику. PlanFuzz уже содержит production-bound экспериментальные адаптеры и несколько oracle families — его нельзя обозначать целиком как отсутствующий.

- `[P0][EXPERIMENTAL]` **Independent Semantic Oracles** — сравнительный результат interpreter/CIL недостаточен, если оба разделяют ошибку; нужны независимые reference observations. `[E]`
- `[P0][NEW]` **Real Bounds-Elimination Test** — реальная Wist/UT IR трансформация удаляет bounds check только при проверенном `SafeIndex`, иначе сохраняет. `[B, E]`
- `[P0][NEW]` **N+1 Composition Test** — отдельно собранные Shape/Range/Bounds packages, zero old-code edits и проверенная precision delta. `[E]`
- `[P0][NEW]` **Evidence Mutation Tests** — заменить бинарную SHA, revision, trust policy, path или mapping и убедиться, что stale result отвергается. `[B, E]`
- `[P0][NEW]` **Cross-IR Negative Tests** — ловить неверный 1:N mapping, trap changes, overflow, memory mutation и misordered effects. `[E]`
- `[P0][PARTIAL]` **Module Contract Verification** — расширять уже имеющиеся typed module/Bytecode/AIR checks, явно документируя непрокрытые семантические области. `[O, C]`
- `[P1][PARTIAL]` **PlanFuzz Lifecycle Schedules** — моделировать интерливинги session/disposal/concurrency и уменьшать failing schedules без изменения finding identity. `[H]`
- `[P1][PARTIAL]` **PlanFuzz Fault Operators** — добавить реальные seeded defects для order, hangs, optimizer interactions; faults должны менять систему, а не post-hoc observations. `[H]`
- `[P1][NEW]` **PlanFuzz Comparative Study** — равнобюджетно сравнить со standard program-only fuzzing, random configuration и pairwise sampling. `[F, H]`
- `[P1][PARTIAL]` **Independent External Adapter** — третий не-Wist/non-Acme workload для проверки отсутствия overfitting к существующим двум языкам. `[H]`
- `[P1][NEW]` **Property-Based Semantic Tests** — генерировать случаи из контрактных законов, но не считать свойства доказанными одной декларацией. `[O, E]`
- `[P1][PARTIAL]` **Differential Backend Testing** — расширить текущие Wist/SSA/AIR/interpreter/CIL comparisons на типы, exceptions и effects. `[S, H]`
- `[P1][NEW]` **Transformation Negative Corpus** — фиксированные regressions для FP signed zero, overflow, aliasing, mutation, exceptions и path sensitivity. `[E]`
- `[P1][NEW]` **Language Authoring Holdouts** — реальные незнакомые SDK authors создают независимые языки без подсказок или прямых Wist dependencies. `[B]`
- `[P1][NEW]` **Editor Revision Tests** — valid→invalid→valid, cancellation, stale result suppression и согласие LSP с batch binder. `[B, P]`
- `[P1][NEW]` **Agent Mutation Tests** — провоцировать неверные edits, unauthorized actions, stale approvals и поддельные доказательства. `[K, P]`
- `[P2][PARTIAL]` **Contract-Derived Testing** — развивать ModuleContracts/ContractExperiments до независимых witness validation, сохраняя experimental status. `[V, O]`

**Критерий приёмки correctness-critical работ:** ноль false-safe трансформаций на негативных тестах; отдельные записи flaky/inconclusive/infrastructure. Минимизация finding должна сохранять exact fingerprint, а не только класс симптома.

### 11. Security, Trust & Reliability

**Смысл:** текущие runtime policies и exact package manifests — полезная защита от ошибок конфигурации, но **не** sandbox и не гарантия добросовестности исполняемой сборки. Любая продвинутая семантическая безопасность требует отдельной trust policy.

- `[P0][PARTIAL]` **Executable Provenance** — identity реальных selected implementations, не только package manifest и канонического плана. `[I, U]`
- `[P0][NEW]` **Proof Trust Enforcement** — ограничить safety-sensitive transformations verified evidence и проверять декларации расширений независимыми средствами. `[E, K]`
- `[P0][NEW]` **Strict Evidence Revocation** — изменения программы, исходного бинарника, trust и scopes гарантированно отменяют зависимое разрешение действия. `[I]`
- `[P1][PARTIAL]` **Runtime Reentrancy Contracts** — дополнить lifecycle/disposal правила thread-safety и явным ограничением concurrent operations. `[F, G]`
- `[P1][PARTIAL]` **Determinism Testing** — permutations providers, registrations, route order и package updates не должны скрыто менять выбранную конфигурацию. `[C, H]`
- `[P1][PARTIAL]` **Diagnostic Redaction** — не допускать чувствительные данные в trace exception messages, логах и sharing/viewer exports. `[U, T]`
- `[P1][NEW]` **Resource Quotas** — внешние CPU/wall-time/memory/network/filesystem ограничения для исполнения недоверенных программ. `[U]`
- `[P1][NEW]` **Policy Edit Authorization** — отделять compiler validation от разрешения host на активацию/исполнение пользовательского правила. `[P]`
- `[P1][NEW]` **Snapshot Concurrency Control** — CAS/transaction semantics для правок, preview и approval на точной версии источника. `[P]`
- `[P2][NEW]` **Out-of-Process Plugin Isolation** — выделенный worker/OS policy только после явного hostile-extension threat model. `[F, U]`
- `[P2][NEW]` **Signed Extension Policy** — проверяемые происхождение и разрешение поставщика пакета в поддерживаемых сценариях внешних plugins. `[F]`
- `[P2][PARTIAL]` **Reproducible Rollback** — хранить exact package set, plan, input schema и предыдущие утверждённые правила для восстановления. `[W, P]`

**Запрет на overclaim:** `RequireDeterminism`, `AllowHostInterop=false` и `PlanHash` не делают исполняемый код безопасным при наличии злонамеренного plugin. Подпись/проверка package identity также не доказывает семантическую эквивалентность.

### 12. Developer Experience, APIs & Package Ecosystem

**Смысл:** существующий alpha SDK уже позволяет независимую разработку языков, но потребительские миграции, документация, NuGet distribution и удобство authoring ещё требуют зрелости. Версию source candidate нельзя называть опубликованной.

- `[P0][PARTIAL]` **API Compatibility Baseline** — определить поддерживаемые публичные контракты и checks для независимых consumers прежде, чем объявлять stable 1.0. `[L, W]`
- `[P1][PARTIAL]` **Builder Ergonomics** — сократить обязательный boilerplate у нынешних Language*Builder без скрытой регистрации/второго состояния. `[G, K]`
- `[P1][NEW]` **Generated API Reference** — создавать справочник по текущему публичному API, привязанный к проверяемым сборкам и версиям. `[L]`
- `[P1][PARTIAL]` **NuGet Publication** — подготовить отдельные проверенные generic SDK artifacts/publication, не путать локальный pack и NuGet.org. `[C, G]`
- `[P1][PARTIAL]` **Package Migration Tooling** — сравнивать identities/contracts/providers/routes stored definitions и выдавать explicit migration diagnostics. `[W]`
- `[P1][PARTIAL]` **Version Compatibility Tests** — внешние старые проекты с новыми пакетами, заранее объявленные allowed/breaking изменения. `[W]`
- `[P1][PARTIAL]` **External Author Quickstart** — свести создание независимого языка к короткому чистому сценарию restore→run→extend. `[G]`
- `[P1][PARTIAL]` **Language Templates** — расширить проверенный `ut-language` тестами и реальными шаблонными языками, а не лишь token substitution. `[G]`
- `[P1][NEW]` **Source Generator Evaluation** — сравнить manual descriptors и generated metadata по changed LOC, errors и consumer compatibility. `[B, F]`
- `[P1][PARTIAL]` **Clean Consumer Verification** — проверять восстановление только из `.nupkg`, не подпитывать тест скрытыми project references. `[G, W]`
- `[P1][PARTIAL]` **Testing Helpers** — упрощённые проверки diagnostics, feature conflicts, route parity и lifecycle для внешних авторов. `[G]`
- `[P2][NEW]` **Contributor Cookbook** — полнофункциональные рецепты grammar/parser, binder, semantic provider, guarded pass, backend и debug adapter. `[P, G]`
- `[P2][PARTIAL]` **Migration Diagnostics** — понятные сообщения по старым manifest schemas и несовместимым API/IDs; не подменять миграцию silent coercion. `[W]`
- `[P2][NEW]` **External IDE Integration Samples** — демонстрации VS Code/браузера поверх одного `LanguageServices` contract. `[P]`

**Идентичность релиза:** ранее в документах указан `UniversalToolchain.Wist` `0.1.0-alpha.1` как опубликованный smoke-tested пакет, а `0.1.0-alpha.7` как неопубликованный source candidate. Перед публикацией заново проверять фактический feed, а не полагаться на этот датированный снимок.

### 13. Product, Adoption & Ecosystem

**Смысл:** проверять, создаёт ли инфраструктура ценность для реальных авторов и конечных пользователей. Продуктовый track может стать полезным даже если обобщённый semantic evidence service не докажет превосходство над typed interfaces.

- `[P1][EXPERIMENTAL]` **Policy/Pricing DSL** — превратить Acme/Wist демонстрации в контролируемый embedded rules use case с наблюдаемой пользовательской пользой. `[P, J]`
- `[P1][NEW]` **Restricted Rules Runtime** — ограниченные host symbols, детерминированный interpreter и отсутствие произвольного I/O. `[P]`
- `[P1][NEW]` **Versioned Rule Documents** — подписываемые/воспроизводимые версии правил с validation, approval и audit metadata. `[P]`
- `[P1][NEW]` **End-User Rule Workflow** — edit→validate→preview→approve→execute→audit→rollback как непротиворечивый workflow. `[P]`
- `[P1][NEW]` **Language Author Journey** — независимый автор создаёт grammar/binder/backend, публикует пакет и подключает минимальное editor tooling. `[P]`
- `[P1][NEW]` **Second Language Family** — configuration/tensor/stream, семантически отличный от pricing/Wist, выявляет скрытые языковые assumptions. `[B]`
- `[P1][NEW]` **Real Integration Pilots** — минимум два независимых использования с измеренной корректностью и издержками настройки. `[J]`
- `[P1][NEW]` **Authoring Usability Study** — шаги настройки, LOC, диагностика ошибок и время до первого валидного DSL. `[J]`
- `[P2][NEW]` **Design Partner Interviews** — пять исследовательских интервью о реальных проблемах .NET-DSL команд; не подменять спрос теорией. `[J]`
- `[P2][NEW]` **Extension Ecosystem Pilot** — разные авторы публикуют и совместно подключают independently versioned extensions. `[J, F]`
- `[P3][UNKNOWN]` **Commercial Support Model** — платная поддержка/интеграция как отдельная бизнес-гипотеза после пилотов, не доказанный рынок. `[J]`

**Базовая стратегия:** узкий embeddable Rules/Policy DSL с полноценным editing-preview-execution циклом — конкурентный практический тест; «полный IDE workbench для всех языков» слишком объёмен до validation спроса.

### 14. Research & Open Hypotheses

**Смысл:** добиться возможности *отвергнуть* интересную архитектуру по результатам независимого эксперимента. Репозиторный research dossier прямо допускает «оставить обычный typed SDK» как успешный исход при меньшей сложности.

- `[P0][EXPERIMENTAL]` **E1 Semantic Interoperability** — превосходят ли scoped facts по независимости и корректности typed C# interfaces/ручные adapters? `[E]`
- `[P0][NEW]` **E2 Cross-Representation Proof** — реально ли переносить факты между AST/AIR/SSA через проверенные correspondence witnesses? `[E]`
- `[P0][NEW]` **E3 Legality Before Cost** — на одном frozen plan показать разные легальные transforms для разных programs без replanning. `[E]`
- `[P1][EXPERIMENTAL]` **E4/E5 Language Workbench** — проверить удобство grammar/binder/editor на втором семантически независимом языке. `[E]`
- `[P1][NEW]` **E6 Full Cost Comparison** — сравнить planning, compilation, allocations, first call, steady-state и amortization с альтернативами. `[E]`
- `[P0][NEW]` **E7 Adversarial Trust** — поддельные `Pure`, stale implementation hashes, contradictions, incomplete dependency sets и malicious provider. `[E]`
- `[P1][PARTIAL]` **E8 Reproducibility** — exact SHA, SDK, OS, seeds, commands, raw logs, target environment и independent holdouts. `[E]`
- `[P1][EXPERIMENTAL]` **Minimal Semantic Ontology** — минимальная схема, объясняющая два независимых языка, без бесконечного реестра почти одинаковых предикатов. `[V, D]`
- `[P1][EXPERIMENTAL]` **Direct Interfaces Baseline** — тот же held-out workload с прямыми typed C# interfaces, без централизованной semantic evidence инфраструктуры. `[E, J]`
- `[P1][NEW]` **MLIR-Style Baseline** — сравнить с operation interfaces, explicit conversion и обычным backend-specific analysis. `[E, J]`
- `[P1][EXPERIMENTAL]` **DSL Evolution Study** — углубить воспроизводимый контрпример PR #370: на малых фиксированных случаях shared handwritten pipeline может быть проще. `[PR370]`
- `[P1][NEW]` **Code Complexity Measurement** — измерить changes to old packages, glue/adapters, code volume, topology и central source edits. `[E]`
- `[P1][NEW]` **External Replication** — независимые авторы повторяют эксперименты и находят новое поведение, не присутствующее в frozen corpus. `[J]`
- `[P2][NEW]` **Incremental Query Engine Study** — проверить, нужен ли dependency-index/cache для IDE/семантики сверх простой immutable recomputation. `[D]`
- `[P2][NEW]` **Bounded Rule Engine Study** — сопоставить прямые интерфейсы с ограниченным proof derivation при реальных транзитивных запросах. `[D]`
- `[P3][UNKNOWN]` **Scientific Differentiation** — доказать отличие от MLIR, ableC, Soufflé, LLVM, Truffle, Salsa и e-graphs без заявления об изобретении typed facts. `[J]`

**Результат исследования:** не «сделано больше abstractions», а воспроизводимый прирост правильности/authoring-cost/интероперабельности в независимых сценариях. Доказанное отсутствие такого выигрыша — корректный итог эксперимента.

### 15. Infrastructure, CI/CD & Technical Debt

**Смысл:** поддерживать дисциплину source ownership, честность документации, package release и воспроизводимость CI при добавлении новых возможностей. Не восстанавливать удалённые legacy topology и не ослаблять negative gates ради зелёного билда.

- `[P0][PARTIAL]` **Source/Docs Consistency** — актуализировать current truth по исходникам и тестам, отделяя исследовательские документы от shipped behavior. `[C, L]`
- `[P0][PARTIAL]` **Schema Documentation Drift** — устранить встречающиеся v5/v6 lock schema упоминания и синхронизировать их с real serialization source. `[C, W]`
- `[P0][PARTIAL]` **Wist Representation Debt** — продолжать вытеснять compatibility AST projections и скрытые module-visitor coupling после уже сделанного phase ownership split. `[C, O]`
- `[P1][PARTIAL]` **Bytecode Representation Migration** — data-only Bytecode/ResolvedBytecode разделение обсуждать отдельно от ещё не влитого PR #371. `[PR371]`
- `[P1][PARTIAL]` **Contract Negative Gates** — расширить существующие мутанты и инварианты на semantic evidence, parsing, LSP, AI tooling. `[O, B]`
- `[P1][PARTIAL]` **Exact-HEAD CI Evidence** — закреплять run/result за commit/tree, не считать зелёный PR на предыдущем SHA проверкой нового. `[C, E]`
- `[P1][PARTIAL]` **CI Compatibility Matrix** — Windows/Linux, external consumers, pack/restore, schema migration и API compatibility на точной ревизии. `[W]`
- `[P1][PARTIAL]` **Package Release Readiness** — separate decisions: source candidate, produced `.nupkg`, reviewed baseline и опубликованный feed. `[C]`
- `[P1][PARTIAL]` **Documentation Testing** — стабильные snippets, архитектурные ссылки, source/release markers и negative documentation checks. `[L, G]`
- `[P1][PARTIAL]` **Benchmark Coverage** — воспроизводимые performance reports и regression controls, в том числе вне hot delegate path. `[R]`
- `[P1][PARTIAL]` **Runtime Failure Diagnostics** — детерминированные неполные отказы, disposal и ошибки exact-route materialization без скрытого fallback. `[C, G]`
- `[P2][NEW]` **Planning Scale Tests** — стресс-графы 100/1000+ contributions, cycles и одинаковой стоимости routes под честным benchmark. `[F]`
- `[P2][NEW]` **Build/Ownership Simplification** — убирать hidden/transitive assembly dependencies, не возвращая S12/S13 retired architecture. `[C]`
- `[P3][NEW]` **Repository Split** — выделение Wist/UT/PlanFuzz только после выполнения split-readiness и обнаружения реальных организационных проблем. `[F]`

**Замечание:** часть документов older snapshot имеет устаревшие численности тестов и схемы. Для будущих изменений считать источником истины проверенную исходную реализацию, текущий test manifest и exact-head CI, а не повторять исторические цифры.

---

## II. Что уже реализовано — не разрабатывать заново

**Подтверждённый фундамент на зафиксированном snapshot:**

1. **Единственная глобальная композиция:** `LanguageDefinition → LanguageCompiler → immutable LanguagePlan → LanguageRuntime`.
2. **Typed packages и contributions:** зависимости, конфликты, slots, replacements, capabilities, выбираемые providers, порядок passes.
3. **Typed artifact routing:** исполнимые conversion routes, pass insertion, exact `(contribution, backend, input contract)` executor, planning-only configurations.
4. **Точная сборка runtime:** проверка selected manifest, runtime-provider identity, lifecycle, `PerSession` и явно stateless singleton; детерминизм и host-interop policy.
5. **Wist compiler:** настоящие syntax/semantic/Bytecode/AIR boundaries, интерпретатор и CIL, фасад `WistEngine`, диагностика и ограниченные presets.
6. **SSA alpha:** opt-in AIR↔SSA, verifier, callable descriptors, managed-call bridge, проверенные на поддерживаемом подмножестве passes — constant folding, SCCP-lite, локальный CSE, dead-pure elimination и branch cleanup.
7. **Низкоуровневый язык-авторинг:** `LanguagePackageBuilder`, `LanguageDefinitionBuilder`, source template `ut-language`, независимый Acme pricing sample.
8. **Частичная наблюдаемость:** `wistc --trace` с versioned/redacted JSON, плановые diagnostics, Wist SSA optimization report.
9. **Тестовая инфраструктура:** плановые, runtime, module/Bytecode/AIR проверки, независимый Acme/Wist PlanFuzz, семь oracle families, replay, deterministic reduction, исторические regression cases.
10. **Release/ownership controls:** проектные boundary checks, clean-consumer smoke, docs checks и benchmark workflows.

**Специально не возвращать как новые bugfix-задачи:** закрытые issues [#302](https://github.com/Misha1302/UniversalToolchain/issues/302), [#303](https://github.com/Misha1302/UniversalToolchain/issues/303), [#307](https://github.com/Misha1302/UniversalToolchain/issues/307). В PlanFuzz status документированы owner-layer fixes и regression cases. Это не снимает обязательств по расширению тестового пространства.

**Не считать mainline implementation:** [PR #371](https://github.com/Misha1302/UniversalToolchain/pull/371) (declarative bytecode handlers, незамёржен) и [PR #370](https://github.com/Misha1302/UniversalToolchain/pull/370) (draft DSL evolution experiment). Их статус в реестре — research/candidate.

## III. Что необходимо именно для заявленных свойств Ideal UT

Следующие **необходимые свойства** не означают, что обязательно надо создавать самостоятельную большую подсистему для каждого. Их желательно покрыть наименьшим набором владельцев и контрактов.

1. **Независимая семантическая композиция.** Несвязанные implementation assemblies должны уметь безопасно предоставить знание неизменённому потребителю; сначала сравнить с typed C# baseline.
2. **Точное время и область истинности знаний.** `ProgramSnapshot`, selected executable digests, backend, phase, program point, assumptions и trust policy — как единые условия оценки.
3. **Корректное fail-closed поведение.** `Unknown`, `Disproven`, `Contradiction`, неполные dependency sets и stale results не позволяют destructive transformation.
4. **Проверяемые обязанности преобразования.** `requires / produces / preserves / invalidates`, independent oracle и точный observation model; legality до стоимости.
5. **Честные границы представлений.** Wist AST, Bytecode и AIR не равны SSA; перенос semantic evidence допустим только через checked witness либо fresh verification.
6. **Полезный внешний language-authoring опыт.** Не только typed package composition, но parser, binder, types, operations, diagnostics и самостоятельное исполнение второго независимого языка.
7. **Общие compiler/IDE/tooling services.** Один владелец семантики и versioned projections для LSP, CLI, web и агентных адаптеров.
8. **Воспроизводимые сравнения с сильными альтернативами.** Handwritten pipeline и typed C# interfaces — равноправные baseline, а не слабые strawmen.

**Самое важное различие:** *«доказать, что семантическая композиция ценна»* — обязательный вопрос исследования; *«внедрить глобальный semantic fact store»* — условный вариант ответа, который может быть отвергнут.

## IV. Что опционально, отложено или исследуется

| Возможность | Когда есть основания инвестировать | Чего не следует заявлять заранее |
|---|---|---|
| MCP/AI tooling | Конкретные сценарии ассистентов с измеримым качеством/refactoring safety | Что AI имеет право подтверждать optimizer correctness |
| Web UI и графический editor | Реальный end-user workflow и польза поверх text-first DSL | Что граф — главный редактируемый семантический источник |
| Source generators | Снижение ручных registrations, ошибок или измеримый AOT-use-case | Что генерация автоматически обеспечивает правильные contracts |
| DAP/CIL debugging | Нужен пользователям после появления interpreter debug hooks | Что optimized variables полностью доступны для stepping |
| SIMD/vectorization | Реальные workloads, typed proofs и независимые correctness tests | Что пример LangDev — уже продуктовый vectorizer |
| SSA-native / optimized-cil backend | Измеримый выигрыш над CIL и приемлемая цена компиляции | Что SSA всегда ускоряет программу |
| Advanced fact inference/Datalog | ≥2 нетривиальных транзитивных сценария, которые нельзя лучше решить interfaces | Что general rule engine нужен для каждого языка |
| Planner cache/incremental planning | Воспроизведённый bottleneck при больших конфигурациях | Что сложная cache invalidation уже окупилась |
| Rich version solver | Реальные несовместимые third-party packages | Что UT должен стать NuGet-заменой |
| Process sandbox | Нужна поддержка hostile/untrusted packages и определён threat model | Что in-process runtime policy уже изолирует злоумышленника |
| Repository split | Dependency/release ownership стало системной проблемой | Что физическое разделение само исправляет архитектуру |
| Flame integration | Есть лицензирование, корректный AIR lowering и сопоставимый benchmark | Что Apache-2.0 проект может автоматически распространять GPL-зависимость |

## V. Что не стоит реализовывать

- **Второй глобальный planner** или runtime rediscovery, обходящий `LanguageCompiler` и immutable `LanguagePlan`.
- **Обязательный универсальный SSA/IR для всех языков:** generic artifact contracts уже намеренно representation-neutral.
- **Безграничную mutable ontology и огромную библиотеку предикатов** до конкретных независимых consumers.
- **Неограниченный логический движок / SAT solver** просто ради архитектурной симметрии.
- **Machine-readable «proof» как доказательство безопасности по определению:** provider может лгать или ошибаться.
- **AI-created `Proven` facts, молчаливое применение patches или запуск code по подсказке модели** без trust/authorization.
- **Объявление валидного плана sandbox.** Для недоверенного исполняемого кода нужна внешняя process/OS граница.
- **Дублирование syntax/semantic source of truth** в LSP, Web UI, trace viewer, agent tools или source generator.
- **Восстановление LogsViewer/`logs.txt`, BasicCore end-to-end orchestrator и retired runtime-manifest topology.**
- **Ранний package/version solver и multi-repo split** при отсутствии проблемы, которую нельзя решить проще.
- **Universal zero-cost, universal compatibility или formal-soundness claims** без соответствующего evidence и доменных ограничений.

## VI. Собственные рекомендации — вне зафиксированного проектного backlog

Эти пять пунктов являются **новыми рекомендациями**, а не пересказом утверждённых research tasks; в число 222 основного инвентаря они не входят:

- `[P1][UNKNOWN]` **Public Capability Matrix** — machine-readable `supported/partial/experimental/unsupported` по каждой package/release identity; уменьшает drift между README и code.
- `[P1][UNKNOWN]` **Contract Compatibility Diff** — сравнение public artifact/semantic contracts и планируемого поведения между releases; заранее выявлять breaking migration.
- `[P2][UNKNOWN]` **Minimal Ecosystem Conformance Badge** — понятный отчёт о прохождении внешним SDK-пакетом contract tests, без маркетинговых гарантий безопасности.
- `[P2][UNKNOWN]` **Agent Tool Conformance Suite** — переносимый набор негативных tests MCP schemas, scopes, authorization, stale revisions и rollback.
- `[P2][UNKNOWN]` **End-to-End Authoring Cost Dashboard** — отдельно отслеживать first-language time, change LOC, downstream breakages и cost of upgrading packages.

## VII. Критические зависимости и инварианты (не календарный roadmap)

| Направление | Что уже должно быть истинно / быть установлено | Что запрещено обходить |
|---|---|---|
| Semantic evidence | Exact selected world, immutable snapshot, schema/trust owner, независимый oracle | `Unknown` → safe, mutable global facts, second planner |
| Cross-IR evidence | Реальный mapping witness, проверенные exceptions/effects и IR revision | Считать source map semantic proof |
| Guarded optimizer | Trusted premises, failsafe baseline, negative corpus | Profitability раньше legality |
| Language services | Единый binder/symbol model и source spans | Повторная независимая IDE-семантика |
| LSP incremental | Document versions, cancellation, stale-result suppression | Применение ответа для `r` к `r+1` |
| AI/MCP editing | Read-only preview, auth policy, exact revision precondition | Самовольное изменение active program или plan |
| Runtime specialization | Frozen selected route + verified obligations + parity | Registry/provider lookup в критическом hot loop по умолчанию |
| Untrusted execution | Отдельный OS/process threat model и hard quotas | Обещание sandbox на основании manifest/policy |
| Public SDK evolution | Clean consumer, compatibility receipts, schema migration | Переписывание старого lock без re-planning |
| Research promotion | UT-integrated experiment, holdouts, direct-interface baseline | Перенос toy model result в production status |

### Почему обычный typed interface — действительно серьёзный конкурент

Для фиксированного DSL и известного набора компонентов прямые C# interfaces, handwritten compiler pipeline и обычная DI-композиция проще и часто требуют меньшего объёма кода. Центральный evidence service оправдан **не** красотой графа фактов, а узким подтверждённым преимуществом: новый независимый producer повышает точность неизменённого consumer, stale evidence правильно отзывается, а суммарная стоимость интеграции и эксплуатации остаётся приемлемой. Исследование [PR #370](https://github.com/Misha1302/UniversalToolchain/pull/370) отдельно показывает, что shared downstream compiler reuse не уникален для UT; нельзя использовать его как доказательство исключительности платформы.

## VIII. Контроль полноты и статистика

**Основной реестр:** 15 тематических направлений, **222 пункта**. Дополнительно: 5 новых собственных рекомендаций, не включённых в основной подсчёт. Все 15 разделов предыдущего ответа сохранены с их приоритетами/статусами; к пунктам добавлены мотивации, ограничения и source keys.

| Направление | Количество |
|---|---:|
| 1. Core Architecture & Composition | 14 |
| 2. Semantic System & Evidence | 20 |
| 3. IR, SSA & Optimizations | 22 |
| 4. Language Authoring & Language SDK | 16 |
| 5. IDE, LSP & Incremental Language Services | 12 |
| 6. Debugging, Diagnostics, Tracing & Visualization | 12 |
| 7. MCP & AI-Agent Tooling | 14 |
| 8. CLI, Web UI & User Workflows | 13 |
| 9. Execution, Backends & Performance | 15 |
| 10. Testing, Verification & Correctness | 17 |
| 11. Security, Trust & Reliability | 12 |
| 12. Developer Experience, APIs & Package Ecosystem | 14 |
| 13. Product, Adoption & Ecosystem | 11 |
| 14. Research & Open Hypotheses | 16 |
| 15. Infrastructure, CI/CD & Technical Debt | 14 |
| **Всего** | **222** |

**По статусу:** `NEW: 132`, `PARTIAL: 73`, `EXPERIMENTAL: 14`, `UNKNOWN: 3`, `DONE: 0` **в основном перечне будущих задач** (уже завершённые функции вынесены в раздел II).\
**По оценочному приоритету:** `P0: 51`, `P1: 127`, `P2: 36`, `P3: 8`.

**Ограничения проверки:** этот документ — расширенная, структурированная фиксация предыдущего аудита репозитория, а не независимая новая перепроверка всего кода. Существование упоминания/документа/PR не является доказательством поддержки API. Статусы и приоритеты необходимо пересматривать при изменении HEAD, результатах экспериментов и появлении настоящих внешних пользователей.

---

## IX. Источники: точные ссылки

Далее `BASE = https://github.com/Misha1302/UniversalToolchain/blob/002a71f0fcdabe7915ed139b297b3829b8060dc2/`. Все ссылки привязаны к проверенной ревизии; `PR370` и `PR371` ссылаются на отдельные ветки, **не** на `master`.

### A. Основной Ideal UT dossier: все 14 файлов

1. [`README.md`](https://github.com/Misha1302/UniversalToolchain/blob/002a71f0fcdabe7915ed139b297b3829b8060dc2/internal-docs/research/ideal-ut-2026-10-08/README.md) — общая постановка исследования, недоказанность предложенных API.
2. [`CURRENT_VS_IDEAL.md`](https://github.com/Misha1302/UniversalToolchain/blob/002a71f0fcdabe7915ed139b297b3829b8060dc2/internal-docs/research/ideal-ut-2026-10-08/CURRENT_VS_IDEAL.md) — IUT-01…13 и границы исходного revision-bound аудита.
3. **`[I]`** [`ARCHITECTURE.md`](https://github.com/Misha1302/UniversalToolchain/blob/002a71f0fcdabe7915ed139b297b3829b8060dc2/internal-docs/research/ideal-ut-2026-10-08/ARCHITECTURE.md) — ownership, identity, evidence, obligation, guarded transformations.
4. **`[D]`** [`IDEAS.md`](https://github.com/Misha1302/UniversalToolchain/blob/002a71f0fcdabe7915ed139b297b3829b8060dc2/internal-docs/research/ideal-ut-2026-10-08/IDEAS.md) — try/prototype/defer/reject и альтернативы.
5. [`ROADMAP.md`](https://github.com/Misha1302/UniversalToolchain/blob/002a71f0fcdabe7915ed139b297b3829b8060dc2/internal-docs/research/ideal-ut-2026-10-08/ROADMAP.md) — gated experiments M0–M6; не календарные обязательства.
6. **`[B]`** [`ENGINEERING_BACKLOG.md`](https://github.com/Misha1302/UniversalToolchain/blob/002a71f0fcdabe7915ed139b297b3829b8060dc2/internal-docs/research/ideal-ut-2026-10-08/ENGINEERING_BACKLOG.md) — условные задачи S0–S8 и определения готовности.
7. **`[E]`** [`EXPERIMENTS.md`](https://github.com/Misha1302/UniversalToolchain/blob/002a71f0fcdabe7915ed139b297b3829b8060dc2/internal-docs/research/ideal-ut-2026-10-08/EXPERIMENTS.md) — E1–E8, отрицательные cases, сравнение с альтернативами.
8. [`LANGDEV.md`](https://github.com/Misha1302/UniversalToolchain/blob/002a71f0fcdabe7915ed139b297b3829b8060dc2/internal-docs/research/ideal-ut-2026-10-08/LANGDEV.md) — реальный Wist demonstration vs proposed SIMD/proof DAG.
9. [`ORIGINS.md`](https://github.com/Misha1302/UniversalToolchain/blob/002a71f0fcdabe7915ed139b297b3829b8060dc2/internal-docs/research/ideal-ut-2026-10-08/ORIGINS.md) — историческая мотивация; не независимая доказательная хронология.
10. [`SOURCES.md`](https://github.com/Misha1302/UniversalToolchain/blob/002a71f0fcdabe7915ed139b297b3829b8060dc2/internal-docs/research/ideal-ut-2026-10-08/SOURCES.md) — происхождение исследований и границы доказательств.
11. [`RESEARCH_TRACEABILITY.md`](https://github.com/Misha1302/UniversalToolchain/blob/002a71f0fcdabe7915ed139b297b3829b8060dc2/internal-docs/research/ideal-ut-2026-10-08/RESEARCH_TRACEABILITY.md) — исходные 14 deliverables, 20 closure conditions, следы AI-гипотез.
12. **`[J]`** [`STRATEGY_PRIOR_ART.md`](https://github.com/Misha1302/UniversalToolchain/blob/002a71f0fcdabe7915ed139b297b3829b8060dc2/internal-docs/research/ideal-ut-2026-10-08/STRATEGY_PRIOR_ART.md) — альтернативы, приор-арт, продуктовые гипотезы.
13. **`[K]`** [`PROPOSED_API_CONTRACTS.md`](https://github.com/Misha1302/UniversalToolchain/blob/002a71f0fcdabe7915ed139b297b3829b8060dc2/internal-docs/research/ideal-ut-2026-10-08/PROPOSED_API_CONTRACTS.md) — **проектные**, не compile-verified API.
14. **`[P]`** [`LANGUAGE_ENGINEERING_PLATFORM.md`](https://github.com/Misha1302/UniversalToolchain/blob/002a71f0fcdabe7915ed139b297b3829b8060dc2/internal-docs/research/ideal-ut-2026-10-08/LANGUAGE_ENGINEERING_PLATFORM.md) — journeys, LSP, DAP, Web UI, agent editing.

### B. Текущая архитектура и language-authoring

- **`[C]`** [`docs/CURRENT_ARCHITECTURE_STATUS.md`](https://github.com/Misha1302/UniversalToolchain/blob/002a71f0fcdabe7915ed139b297b3829b8060dc2/docs/CURRENT_ARCHITECTURE_STATUS.md) — текущая authority архитектуры.
- **`[L]`** [`docs/limitations.md`](https://github.com/Misha1302/UniversalToolchain/blob/002a71f0fcdabe7915ed139b297b3829b8060dc2/docs/limitations.md) — честные ограничения релизных поверхностей.
- **`[F]`** [`docs/architecture/future-work.md`](https://github.com/Misha1302/UniversalToolchain/blob/002a71f0fcdabe7915ed139b297b3829b8060dc2/docs/architecture/future-work.md) — activation triggers, отложенные решения и explicit rejected complexity.
- **`[G]`** [`docs/language-authoring/index.md`](https://github.com/Misha1302/UniversalToolchain/blob/002a71f0fcdabe7915ed139b297b3829b8060dc2/docs/language-authoring/index.md) — уже существующий generic SDK и ограничения.
- **`[W]`** [`docs/language-authoring/versioning-and-migrations.md`](https://github.com/Misha1302/UniversalToolchain/blob/002a71f0fcdabe7915ed139b297b3829b8060dc2/docs/language-authoring/versioning-and-migrations.md) — миграции и идентичности контрактов.
- [`docs/language-authoring/`](https://github.com/Misha1302/UniversalToolchain/tree/002a71f0fcdabe7915ed139b297b3829b8060dc2/docs/language-authoring) — также quickstart, package-model, contribution-planning, artifact-routing, runtime-lifecycle, testing-and-templates.
- [`docs/architecture/`](https://github.com/Misha1302/UniversalToolchain/tree/002a71f0fcdabe7915ed139b297b3829b8060dc2/docs/architecture) — также external-language-authoring-sdk, composition-explain-plan, IR routing, backends/parity, Wist phase ownership.
- [`readme.md`](https://github.com/Misha1302/UniversalToolchain/blob/002a71f0fcdabe7915ed139b297b3829b8060dc2/readme.md) — публичный Wist вход и границы публикации.
- [`LanguageCompiler.cs`](https://github.com/Misha1302/UniversalToolchain/blob/002a71f0fcdabe7915ed139b297b3829b8060dc2/UniversalToolchain/UniversalToolchain.LanguageSdk/LanguageCompiler.cs), [`LanguagePlan.cs`](https://github.com/Misha1302/UniversalToolchain/blob/002a71f0fcdabe7915ed139b297b3829b8060dc2/UniversalToolchain/UniversalToolchain.LanguageSdk/LanguagePlan.cs), [`LanguageRuntime.cs`](https://github.com/Misha1302/UniversalToolchain/blob/002a71f0fcdabe7915ed139b297b3829b8060dc2/UniversalToolchain/UniversalToolchain.Runtime/LanguageRuntime.cs) — подтверждённые исходные owners планирования и исполнения.
- [`LanguageArtifactRoutePhase.cs`](https://github.com/Misha1302/UniversalToolchain/blob/002a71f0fcdabe7915ed139b297b3829b8060dc2/UniversalToolchain/UniversalToolchain.LanguageSdk/LanguageArtifactRoutePhase.cs) — порядок, стоимость, неоднозначные маршруты (`UTL2207/UTL2208`).

### C. IR / debugger / security / research adjuncts

- **`[M]`** [`docs/architecture/callable-first-ssa.md`](https://github.com/Misha1302/UniversalToolchain/blob/002a71f0fcdabe7915ed139b297b3829b8060dc2/docs/architecture/callable-first-ssa.md).
- **`[S]`** [`docs/architecture/ssa-coverage-matrix.md`](https://github.com/Misha1302/UniversalToolchain/blob/002a71f0fcdabe7915ed139b297b3829b8060dc2/docs/architecture/ssa-coverage-matrix.md).
- **`[X]`** [`docs/architecture/bytecode-and-air.md`](https://github.com/Misha1302/UniversalToolchain/blob/002a71f0fcdabe7915ed139b297b3829b8060dc2/docs/architecture/bytecode-and-air.md).
- **`[T]`** [`docs/architecture/debug-trace-v2.md`](https://github.com/Misha1302/UniversalToolchain/blob/002a71f0fcdabe7915ed139b297b3829b8060dc2/docs/architecture/debug-trace-v2.md) и [`docs/reference/debug-trace-schema.md`](https://github.com/Misha1302/UniversalToolchain/blob/002a71f0fcdabe7915ed139b297b3829b8060dc2/docs/reference/debug-trace-schema.md).
- **`[V]`** [`docs/research/semantic-fact-proof-layer-2026-09-20.md`](https://github.com/Misha1302/UniversalToolchain/blob/002a71f0fcdabe7915ed139b297b3829b8060dc2/docs/research/semantic-fact-proof-layer-2026-09-20.md).
- **`[Q]`** [`docs/start/cli-reference.md`](https://github.com/Misha1302/UniversalToolchain/blob/002a71f0fcdabe7915ed139b297b3829b8060dc2/docs/start/cli-reference.md).
- **`[R]`** [`docs/reference/benchmark-methodology.md`](https://github.com/Misha1302/UniversalToolchain/blob/002a71f0fcdabe7915ed139b297b3829b8060dc2/docs/reference/benchmark-methodology.md).
- **`[U]`** [`docs/SECURITY.md`](https://github.com/Misha1302/UniversalToolchain/blob/002a71f0fcdabe7915ed139b297b3829b8060dc2/docs/SECURITY.md).
- **`[O]`** [`internal-docs/proposals/typed-module-contracts-and-verifiers.md`](https://github.com/Misha1302/UniversalToolchain/blob/002a71f0fcdabe7915ed139b297b3829b8060dc2/internal-docs/proposals/typed-module-contracts-and-verifiers.md).
- **`[H]`** [`internal-docs/proposals/planfuzz/implementation-status.md`](https://github.com/Misha1302/UniversalToolchain/blob/002a71f0fcdabe7915ed139b297b3829b8060dc2/internal-docs/proposals/planfuzz/implementation-status.md) и [issue #298](https://github.com/Misha1302/UniversalToolchain/issues/298).
- **`[Y]`** [`internal-docs/proposals/flame-ssa-optimizing-backend-design/index.md`](https://github.com/Misha1302/UniversalToolchain/blob/002a71f0fcdabe7915ed139b297b3829b8060dc2/internal-docs/proposals/flame-ssa-optimizing-backend-design/index.md).
- **`[PR370]`** [DSL evolution experiment, open draft](https://github.com/Misha1302/UniversalToolchain/pull/370).
- **`[PR371]`** [Declarative Wist Bytecode operations, open PR](https://github.com/Misha1302/UniversalToolchain/pull/371).

### D. Итоговый принцип обновления реестра

При следующем изменении реализации сначала **проверить новый HEAD и новый actual source/CI**, затем обновлять статусы отдельных пунктов. Изменение proposal, наличие новых типов или прохождение toy-tests само по себе не переводит `NEW/EXPERIMENTAL` в `DONE`. Для серьёзного upgrade: exact source revision → внешний consumer → negative oracle → compatibility/release evidence → обновлённый current architecture status.
