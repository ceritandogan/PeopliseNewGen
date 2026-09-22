# Case reports are recomputed on every read, never persisted

`Case.Reports` and `Report.GeneratedAt` read as if a report is a saved snapshot, but the new `GET /api/cases/{caseId}/report` endpoint doesn't persist one: `Case.GenerateReport` runs fresh on every request, building `ReportSectionContent` off whatever `Case.AssessmentResult` and `CaseBotProject.ReportTemplate` currently contain, and returns the result without writing it back. A reviewer viewing the same report twice, after a rescoring happened in between, sees the current state both times — never a stale cached copy.

We considered generating once and caching (`GetCaseReportQuery`'s original, already-written behavior: generate+persist only if `Case.Reports` is empty, otherwise return the same row forever) — rejected because it silently goes stale the moment anything scored after the report's first view changes, with no regenerate action built to fix it. A report a reviewer is actively relying on to make a decision should never be known-wrong without them realizing it.

## Consequences

- No history of what a reviewer actually saw at the moment they viewed it — if that's ever needed for audit/compliance reasons (this codebase has been careful about KVKK concerns in Stages 10 and 12), it isn't here. Recomputation is cheap and deterministic today (no AI call in the path), which is what makes discarding history an acceptable trade rather than a performance necessity.
- `Report`/`ReportSectionContent` as persisted entities are effectively unused by this endpoint — they exist in the domain model (and `Case.GenerateReport` still returns a transient instance of them) but nothing calls `context.SaveChanges` for a `Report` from this read path.
