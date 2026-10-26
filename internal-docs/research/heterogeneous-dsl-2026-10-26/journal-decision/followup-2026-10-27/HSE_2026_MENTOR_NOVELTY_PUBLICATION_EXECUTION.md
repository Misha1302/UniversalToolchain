# UniversalToolchain — HSE FCS 2026 rules, scientific mentors, novelty and Q1/Q2 execution (27 Oct 2026)

**Authority:** research-only source audit / advisory proposal. No production modification, journal submission, paid engagement, or HSE formal determination. Historic baseline PR #382 HEAD ba14e481b14cbf323bfa96f52b469950ab751495; preceding independent decision: [INDEPENDENT_Q1_Q2_AUDIT](INDEPENDENT_Q1_Q2_AUDIT.md). The additional HSE first-party **signed 2026 rules** discovered here supersede the previous unverified assumption that Q1/Q2 acceptance was an absolute HSE bachelor's requirement.

## 1. Executive decision / critical distinction

Do not confuse (1) 2026–2030 Moscow undergraduate **Программная инженерия** degree, (2) separate **Исследовательская программа ФКН** track, (3) separate named **scholarship for A-list publication**, or (4) an individual's special admission/contract condition.

- **Degree:** 09.03.04, 4 years / 240 credits; official 2026–2030 approved study plan. No blanket Q1/Q2 graduation condition found in examined six-page plan.
- **Research track:** binding Faculty order **01.09.2026 No. 2.03-05/010926-1**, official https://www.hse.ru/docs/1196199502.html , **actual DOCX downloaded from that page and extracted on Fedora 2026-10-27**, SHA-256 3f22c25053e4452a8964994e9d05afe452871effe3c28bfd746b53cf5088400c. This is a **supplemental track** to the undergraduate degree. It allocates a real supervisor + research topic, has January and June/July reviews, and can drop an unsuccessful participant **without expelling that person from HSE**.
- **Track's permanent review criteria:** no arrears in professional-module subjects or track supplements, no formal reprimand; plus **ADDITIONAL research criteria determined by the Academic Council in modules 1 and 3 of each year** (paragraphs equivalent to section 5, 5.2–5.3). Reports, presentation and advisor assessment enter the committee decision. The signed 2026 regulation itself imposes **no universal Q1/Q2 requirement**. Publication requirement for any *specific review* = **NOT VERIFIED** until obtain Council-approved extra criteria for next January/June review. Do not claim an absence of an extra Q1 criterion.
- **Selection:** first to third year undergrads may apply; 2026 signed rule says applications open by October 15 and selections published by November 1. The live program webpage says selection near end of module 2 instead: discrepancy needs written clarification from coordinator. Research topic/advisor chosen by **December 1** for newly enrolled; Academic Council approves topic/advisor in personalized education plan. Project/advisor change by written application in September or February and no more than twice per academic year.
- **Excluded track students:** stop track individual-plan enrollment, revert to regular course project; no HSE degree expulsion, written corporate email with grounds.
- **Stipend:** separate **10.08.2026 Faculty order No. 2.03-05/100826-1**, https://www.hse.ru/docs/1188538576.html. Original DOCX downloaded and analyzed (SHA256 715f07a253a2fa0d8a6989657c06e2dd8ba67d876a8142f8411758968a5111fb). Actual base amount is annually approved by dean, NOT fixed in that order; do not quote 2024 promotional 40k as guaranteed 2026. Schedules: first-years Nov–Jan / Feb–Aug, others Sep–Jan / Feb–Aug, conditioned on student status and stipulated exclusions.
- **Separate reward for accepted/published top research:** https://cs.hse.ru/scientific/grants offers **nominal 50,000 RUB/mo in 2026/27** for qualifying A journal / ACONF conference paper, subject to detailed eligibility, faculty award and possible reductions; NOT the track stipend, NOT a blanket Q1/Q2 recognition. For a student with high-tier papers this is a possible additional program, not guaranteed income.

### Immediate authoritative request to HSE

Request the 2026/27 **Academic Council approved additional January and June/July attestation criteria**, deadline/status if publications are demanded, current selection rules and whether UT qualifies as project and formal supervisor eligibility; request a copy of the decision/points. Official track URL https://cs.hse.ru/researchprogram lists manager Vera Volkova (https://www.hse.ru/org/persons/916918530/) and administrator Victoria Dobryakova (https://www.hse.ru/org/persons/1107174511). Degree office sestudyoffice@hse.ru; FCS project center cppr.cs@hse.ru. Do not expose personal academic documents in public PR.

## 2. Mentor sourcing: specific humans and decision roles

Start by seeking **faculty-approved supervisor at zero direct cash price**, not paying for paper placement or a coauthor slot. Team roles can be split (formal FCS supervisor + technical PL critic + empirical-methods reviewer). Recommendations below are **field fit based on official HSE profiles**, not confirmation of availability/willingness/fee:

| Expert | Official profile | Evidence / best scope | Initial request |
|---|---|---|---|
| Viktor V. Kuliamin (В.В. Кулямин) | https://www.hse.ru/org/persons/161646599/ | Associate professor, ISP RAS FCS basic department, software verification and validation / 2026–27 software quality/testing course | Formal supervisor / PlanFuzz+empirical testing advice; ask whether can supervise bachelor's research track |
| Alexey V. Khoroshilov (А.В. Хорошилов) | https://www.hse.ru/org/persons/161646608/ | Verification, requirements, model-based testing; same department | Independent checker/oracle design, falsifiable protocol and a prospective supervisor |
| Andrey A. Belevantsev (А.А. Белеванцев) | https://www.hse.ru/org/persons/501143275/ | Prof. in code optimization, static analysis, parallel programs, compiler expertise | One-session PL/compilers novelty red-team and closest LLVM/MLIR methods, not presumed available for ongoing mentoring |
| Alexander S. Kamkin (А.С. Камкин) | https://www.hse.ru/org/persons/209608913/ | Formal methods, static analysis, software verification; 2026–27 verification course | Independent semantics reference, experimental protocol review |
| Basic department ISP RAS | https://cs.hse.ru/dse/sp/persons/ | Official route to identify current junior researchers with direct hands-on compiler integration capacity | Ask department for an actual researcher with 2025–26 relevant output and capacity |

Suggested contact ordering: Kuliamin + Khoroshilov as first two, Kamkin as independent method/formal critic, Belevantsev for a time-bounded compiler novelty check. A high-prestige professor with 0 hours available is worse than a midcareer expert with real reproducible-study supervision.

**Do not assume any named individual is a paid freelancer.** At HSE https://cs.hse.ru/partners/projects advertises institute-delivered technical consulting and studies under negotiated written contract; institution's project-group limit of up to 2m RUB/6 months is a maximum program parameter, not a quote for a personal advisor. Formal track advisor role must be approved by Academic Council; paying an external consultant cannot substitute institutional approval.

If no FCS supervisor or extra methodological capacity: recruit an independent **scientific reviewer**, not a ghostwriter, from published authors of closest conference papers, relevant PL/SE academic labs and research communities, or specialist consulting platform https://www.kolabtree.com/services/scientific-consulting. Platform displays featured consultants **$90–$150/hour as indicative 2026 marketing prices**, not validated UT/compilers offers. Search candidate CV/Google Scholar/DBLP/ORCID, recent papers (2023–2026), actual availability and written quote. Keep fee scopes small (2–4 hours novelty rejection review, 4–8 hours protocol review) before larger contracts. Ethics: external readers may be acknowledged, collaborators with substantial verifiable scholarly contributions may qualify as coauthors under target journal's rules; money alone never buys authorship, review outcome or editorial connection (COPE https://doi.org/10.24318/cope.2019.1.8; Wiley STVR https://onlinelibrary.wiley.com/page/journal/10991689/homepage/forauthors.html).

### Four-week supervision audition (before contract)

Provide: 2-page English research pitch, PR #382 dossier and four-case limitations, top 10 competitor methods, exact negative result, list of 3 falsifiable RQs, proposed matched manual baseline, HSE gate and precise availability requested (e.g. 30-min first call + fortnightly meetings).

On first call ask: What is the strongest case against the claim? Who exactly published it? What would make the project publishable *even if treatment fails*? Which impartial independent oracle and external target? Which journals did mentor publish/review for? Is there time for review within 7 working days? Can they legitimately serve as FCS track supervisor and what must be approved? Negotiate finite deliverables: novelty matrix, protocol with power/precision after pilot, three reviewed gates, rejection readiness audit. Stop after 4 weeks if only motivational praise without precise critical analysis, no availability, or vague pay-for-publication promises.

## 3. Novelty audit: main direct competitors, not generic buzzwords

| Study/method | Verified distinct established prior contribution | What UT **cannot** credibly claim as new | Remaining conditional UT question |
|---|---|---|---|
| LLVM New Pass Manager | https://llvm.org/docs/NewPassManager.html — cached analyses and preservation/invalidation | 'We selectively invalidate analyses' | Do explicit boundary contracts reduce full maintenance cost with equal checks? |
| MLIR pass manager + interfaces | https://mlir.llvm.org/docs/PassManagement/ and https://mlir.llvm.org/docs/Interfaces/ — preserved analyses, dialect-independent interfaces | 'We can preserve analyses across passes' or 'generic consumers process new dialects' | Correctness/maintenance under independent extension changes and unreliable declarations |
| Fehr et al., First-Class Verification Dialects, PLDI 2025 | https://doi.org/10.1145/3729309 — first-class formal semantics in MLIR, dialect-independent verification | 'Generic semantic verification across DSL dialects' alone | Explicitly weaker but lower-cost contract tradeoff, with empirical proof |
| Lopes et al., Alive2, PLDI 2021 | https://doi.org/10.1145/3453483.3454030 — bounded translation validation | 'We prove arbitrary optimizer semantic preservation' | Bound contracts precisely; oracle must catch shared wrong output |
| Thüm et al., emergent contract interfaces, SPLC 2016 | https://doi.org/10.1145/2934466.2934471 — empirical understanding of product-line changes/contracts | 'Contract interfaces help maintenance' generic claim | Full end-to-end implementation/adapters/schema/docs/cost vs same checker manual typed pipeline |
| ConfigFuzz, TOSEM 2023 | https://doi.org/10.1145/3580597 — joint program input/option configuration fuzzing | 'We fuzz inputs AND configs' | Composition lifecycle transitions and independent exact oracle |
| Tzer, OOPSLA 2022 | https://doi.org/10.1145/3527317 — joint compiler IR and pass mutation | 'Joint input/pass exploration' | Error finding at fixed shared oracle and resource budget |
| VAFuzz, ICSE 2026 | https://conf.researchr.org/details/icse-2026/icse-2026-research-track/188/Variability-Aware-Fuzzing — presence-condition seeds across configurations | 'Variability-aware configuration fuzzing' generically | Independent configuration-lifecycle negative controls |
| SynthFuzz, ICSE 2025 | https://doi.org/10.1109/ICSE55347.2025.00037 — context-aware MLIR mutation synthesis | 'Easily generate test changes across dialects' | Stronger lifecycle-specific findings and no oracle confound |
| LOOL, TOSEM 2026 | https://doi.org/10.1145/3807508 — optimizer-log-guided compiler fuzzing | 'New compiler fuzzing tool' | Independent validation and distinctive software engineering knowledge |

**Novelty is currently NOT VERIFIED.** This is a competitive threat map rather than exhaustive scientific survey: first build a reproducible 2021–2026 DOI corpus using DBLP, ACM DL, IEEE Xplore, Semantic Scholar/OpenAlex, Scopus (institutionally if licensed), Google Scholar and author/reference snowballing; save query, date, source, DOI, full-text access, methods, supported semantics, corpus, oracle, baseline, threats and the *exact claim collision* for all papers. Then ask external PL expert to reject a 1-page proposed contribution statement. An inability to find a close paper by search is not proof of novelty.

Queries in English:
- "semantic contract" AND ("compiler pass" OR "DSL evolution") AND (verification OR invalidation)
- "incremental analysis preservation" AND ("MLIR" OR "LLVM") AND performance
- "modular compiler verification" AND "maintenance" AND empirical
- "compiler configuration" AND "lifecycle" AND fuzzing
- "joint IR-pass mutation" AND compiler testing
- "contract interfaces" AND "maintenance tasks" AND empirical study

Predefine hypotheses **before** manipulating study results. Three immediate refuters: a previously published equal-claim method; matched manual typed baseline matches all outcomes at equal or lower full cost; no genuinely external maintainer/system/material after a bounded sourcing attempt. Under those cases narrow to negative case study or pivot B, never widen novelty claim to save a journal target.

## 4. Experimental anti-rejection program

**Stage 0 (research feasibility only):** six frozen tasks, U=UT typed obligations, M=competent handwritten typed baseline same verifiers/metadata, A=always-check same predicates, O=independent source-reference evaluator. Record all assigned trials before scientific validation, independent oracle catches shared wrong results, declared and undeclared invalidation strata explicit. Six model-designed tasks are *not* final N or independent industry sample.

**Stage 1 (external replication):** seek actual maintainers of 2–3 independent language-processing/DSL frameworks, with permission to use artifacts and report negative results. Include an external .NET extension-based project and, only when realistic, a small MLIR or Roslyn-inspired case. Do not count one UT variant cloned three times as three independent systems. Counterbalance participant order, capture training and context, cluster outcomes by task/project/participant; pilot variance determines power/precision and sample size before confirmatory study. Primary: independent oracle correct completion, full change effort including adapters, schemas, testing, docs, migrations and repair. Secondary: full compiler throughput (cold/warm, same hardware). Keep complete denominator, raw results, environments, signed frozen protocol, negative findings, blind adjudication.

**Stop rules:** (a) same check information not achievable => no comparative claim; (b) independent oracle cannot rule shared bugs out => no correctness claim; (c) matched manual better/no measurable UT effect with robust cost => no superiority claim; (d) no external witness => no multi-project Q1 generality; (e) unavailable HSE attestation criteria => no formal-credit claim. An interesting negative controlled result can still be publishable if mechanistic and generalizable.

### Predictable editor objections and required evidence

| Desk rejection / reviewer criticism | Severity | Required defense |
|---|---|---|
| 'Feature already exists in LLVM/MLIR' | CRITICAL | One-page claim vs nearest methods and significant novel empirical question |
| 'Artificial faults designed by treatment author' | CRITICAL | External independently sourced changes/defects; origin stratification |
| 'Manual baseline crippled' | CRITICAL | Identical predicates/checker sources, equal engineering support and information |
| 'Oracle is same implementation' | CRITICAL | Separate independent semantics and shared-wrong-output negative control |
| '22 observations masquerade as 22 independent bugs' | CRITICAL | Four original cases explicitly labeled and no pseudoreplication |
| 'Metrics optimized post hoc' | CRITICAL | Freeze hypotheses, sample plan and exclusions before confirming |
| 'Tool-specific report, no general SE contribution' | CRITICAL | Independent adopters and explanatory mechanism/negative tradeoffs |
| 'Paper overlaps old CGO results' | CRITICAL | Signed overlap/novel data and citation matrix, author receipt of CGO submission |
| 'Data/Code cannot be replayed' | HIGH | Zenodo/GitHub DOI-tagged version, raw-first scripts, CI/replay, packages |
| 'Journal does not match angle or university credit' | HIGH | Journal editor charter, exact JCR/Scopus year/category and formal HSE instruction |

## 5. Journal choice and cash / timing

For strong multi-project software-maintenance study: **ACM TOSEM** as primary conditional target (2025 JIF Q1 in prior publisher indexed dossier). If PL/formal mechanism robust: TOPLAS may fit (2025 JIF Q2, versus 2024 JIF Q3); do not target it as convenience Q2 substitute if paper is purely empirical. If independent empirical result rather than PL theory, consider **Empirical Software Engineering** (Springer, hybrid) instead. For PlanFuzz testing-specific fallback, **Software Testing, Verification and Reliability** (Wiley ISSN 0960-0833 / 1099-1689) has strong official aims: https://onlinelibrary.wiley.com/page/journal/10991689/homepage/aims.htm and author rules https://onlinelibrary.wiley.com/page/journal/10991689/homepage/forauthors.html; **2025 SJR Q2 shown by third-party aggregator** https://scienceaijournal.com/journals/software-testing-verification-and-reliability-0960-0833, NOT independent authoritative category-specific HSE clearance. Publisher https://onlinelibrary.wiley.com/journal/10991689/ displays an acceptance-rate figure **4%** and median first decision **26 days**, but denominator, cohort and definition not published there: not the individual's acceptance probability or review-to-acceptance timeline.

**ACM major financial correction:** publisher https://prod-libraries.acm.bloomreach.cloud/acmopen/apc-list-pricing explicitly says from Jan 1 **2027** journals processed by eRights revert to regular APC **$1,800 nonmember / $1,300 ACM/SIG member**, unless institution ACM Open participant, qualifying waiver, etc. 2026 subsidies ($1,450/$950 journal rate) should NOT fund a 2027 projected budget. STVR optional hybrid OA https://onlinelibrary.wiley.com/page/journal/10991689/homepage/fundedaccess.html **$2,860** excluding tax (publisher landing page, refresh at acceptance), while non-OA ordinary licensing route can avoid optional OA fee. Springer EMSE likewise has an ordinary subscription route; always verify institution/funder obligations. Prefer USD cost accounting without unverified RUB conversion.

**Funding:** track stipend fixed per current dean order, not yet located; separate 2026/27 FCS distinguished-publication scholarship advertises 50k RUB/mo base for A/ACONF after conditions, not guaranteed and distinct from track. Academic advisor in track can potentially have zero direct fee; paid external specialty consultation is extra. Kolabtree illustrative 2026 $90–150/h: fixed 4h novelty review about **$360–$600**, 8h protocol advisory **$720–$1,200**, purely *illustrative if an eligible consultant actually quotes that price*. Full engineering time of previous plans is an assumption, not actual billing. Contract cash = rate × accepted hours + actual cloud/tool costs + APC if due + any re-review/venue-transfer cash. Track good time to degree = 2026–2030, but each Jan/June research-track review is an independent intermediate deadline; journal accept/review/indexing is not controllable by authors.

## 6. Day/semester execution and decision schedule

| Deadline | Real task and artifact | Objective decision |
|---|---|---|
| Oct–Nov 2026 (ASAP) | Obtain HSE **2026/27 extra review criteria**, current 2026 admission status/requirements; register project/mentor before Dec 1 if accepted; prepare mentor pitch | Do not infer Q1 obligation from generalized program description |
| Next 7 days | Top 10 closest works full methodology, 1-page rejected-claim matrix; two FCS first contacts; six tasks before baseline code; 4-week consulting scope if academic route not yet working | GO to reproducible pilot only if check parity/oracle valid |
| Next 30 days | Small frozen U/M/A/O local feasibility, negative controls, raw data, external maintainer prospect list; supervisor review | If novelty collapses, PIVOT PlanFuzz or stop |
| January 2027 | Track review: research report, reproducible artifact and supervisor review per *current extra criteria*; journal milestones separately | Aim to retain research-track status, not pretend 'submitted' = accepted |
| 60–90 days | External pilot, sample/design recalibration, venue-specific claim and draft skeleton | Confirmatory study only after independent recruitment and protocol freeze |
| Mid 2027 | Broader external study or sharply honest negative case study; thesis/course-project coordination | Journal GO if independent effect; otherwise PIVOT |
| 2028–2030 | Full research article, editor review, revisions, publication or indexing; coordinate WКР | Conditional opportunities, no fixed acceptance guarantee |

Potential degree project 2026/27 website applies primarily to **second/third-year** project students; do not confuse those deadlines with first-year track entry. 2026 basic track order has initial selection by Nov 1, but program webpage says end of module 2—obtain written ruling promptly.

## 7. Email briefs to use (NOT SENT)

**To track program coordinator**:
Subject: 'ИП ФКН 2026/27 — критерии аттестации, научный руководитель и исследовательская тема UT'
'I am on/plan to apply to HSE FCS Software Engineering 2026 intake. I have an existing open-source UniversalToolchain research project on typed semantic contracts / testing of compiler configuration. I studied the FCS rules of 01.09.2026 No. 2.03-05/010926-1. Please send the currently approved additional criteria for the next January and June/July attestation (Academic Council decision), explain whether publication Q1/Q2 is required for this cohort and what submission/acceptance/indexing status, ranking base/year/category and authorship mean. Could a compiler-verification lecturer supervise this topic, who confirms the December 1 assignment, and which 2026 selection timeline prevails over the webpage? I am prepared to submit a 2-page research proposal and evidence from a reproducible pilot.'

**To specific technical faculty mentor**:
Subject: 'Запрос научного руководства / 20-минутной научной консультации — UniversalToolchain, semantic contracts, 2026'
'Здравствуйте! Я занимаюсь исследовательской веткой UniversalToolchain [PR #382]. Есть реальный compiler/runtime pipeline, PlanFuzz and a reproduced negative control: undeclared invalidation defeats selective checks, and incomplete verifiers miss some shared errors. I am not treating this as a publishable result. I want to evaluate a falsifiable maintenance/correctness claim against LLVM/MLIR and a strong identical-check handwritten typed baseline, then seek independently owned projects. Could I send a two-page pitch and ask for a 20–30 minute critical discussion about novelty, suitable experiment and whether you could formally supervise/refer me within the FCS research track? It is important to hear the strongest rejection reasons rather than an endorsement.'

## 8. Evidence ledger / open limitations

- Official HSE program study plan https://www.hse.ru/dbs/education/sp_UnitedLearnPlan_27819.pdf, 2026–2030.
- HSE 2026 signed research-track rule https://www.hse.ru/docs/1196199502.html (DOCX direct download and SHA above, exact source inspected 27 Oct).
- HSE August 2026 track stipend rule https://www.hse.ru/docs/1188538576.html (exact downloaded and SHA above; no amount asserted).
- HSE research track public https://cs.hse.ru/researchprogram (some public timing statements differ from signed September policy).
- HSE track FCS staff and faculty profiles listed above, availability/contract **not** confirmed.
- FCS consulting https://cs.hse.ru/partners/projects (prices negotiated; no personal hire offers verified).
- All science novelty claims remain hypotheses, not final verified novelty; real external sample/data absent.
- HSE **current Academic Council extra review criteria for January/June 2026/27**, individual enrollment/contract, current APC institutional coverage, specific target journal quartile required by this track, current paid mentor quotes, external participant permissions: **UNVERIFIED**. These are concrete next administrative, financial or empirical gates.
