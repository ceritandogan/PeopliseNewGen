# PeopliseNewGen

A multi-tenant HR platform: a modular monolith covering applicant tracking (ATS), an HR chatbot (HrBot), and video-based case interviews (VideoInterview), fronted by a React panel (recruiter-facing) and a separate candidate-facing app.

## Language

**Position**:
A role a tenant is hiring for — title, department, location, work mode, seniority, employment type. A `Position` has **no lifecycle status** (no "open"/"closed"/"filled"). Every `Position` ever created is listed as-is; "open position" is UI copy, not a domain state, until a deliberate future decision introduces one.
_Avoid_: "Open position" as if it names a domain state — it doesn't yet.

**Applicant count**:
The total number of `CandidateProcess` records that have ever entered a `Position`'s pipeline, counted across every `PipelineStatus` including terminal ones (`Rejected`, `Eliminated`, `TimedOut`, `Accepted`). Not a count of candidates currently active in the pipeline — that's a different, not-yet-built metric.
_Avoid_: Using "applicants" to mean "active candidates" without saying so.

**CandidateProcess**:
One candidate's run through one `Position`'s pipeline, tracked via `PipelineStatus` (`NewApplication` → `UnderReview` → `Testing` → `Interviewing` → `Offer` → `Accepted`, or diverted to `Rejected`/`Eliminated`/`TimedOut`).

**Reviewer**:
Not a separate domain entity — a `Reviewer` is simply an authenticated panel `User` in the act of scoring a `Case`. `CaseScoring.ReviewerId` is that user's stable id (the JWT `sub` claim), not a role, a job title, or a client-supplied label. There's no "who is allowed to review" concept yet — any authenticated panel user can submit a score (see the app's broader lack of role administration).
_Avoid_: Treating "Reviewer" as a role/permission concept — it isn't one until a deliberate future decision introduces role-gating.
