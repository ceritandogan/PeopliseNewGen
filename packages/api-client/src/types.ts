/**
 * Hand-typed to mirror the backend's actual Application-layer DTOs (Peoplise.Modules.*).
 * Real HTTP controllers now exist for the thin-slice endpoints (Positions, Candidates,
 * auth), but only for those five — regenerate this file (and the resource modules) with
 * `openapi-typescript` once a live `swagger.json` covers the rest of the surface. Keep
 * these in sync by hand until then.
 */

// ---- ATS ----------------------------------------------------------------

export type WorkMode = "Office" | "Remote" | "Hybrid";
export type SeniorityLevel = "Intern" | "Junior" | "Mid" | "Senior" | "Lead" | "Principal";
export type EmploymentType = "FullTime" | "PartTime";
export type PipelineStatus =
  | "NewApplication"
  | "UnderReview"
  | "Testing"
  | "Interviewing"
  | "Offer"
  | "Accepted"
  | "Rejected"
  | "Eliminated"
  | "TimedOut";

export interface CreatePositionRequest {
  title: string;
  department: string;
  city: string;
  country: string;
  workMode: WorkMode;
  seniorityLevel: SeniorityLevel;
  employmentType: EmploymentType;
}

export interface PositionDashboard {
  positionId: string;
  title: string;
  totalApplicants: number;
  applicantsByStatus: Partial<Record<PipelineStatus, number>>;
}

export interface PositionListItem {
  positionId: string;
  title: string;
  department: string;
  city: string;
  country: string;
  applicantCount: number;
}

export interface SubmitCandidateApplicationRequest {
  /**
   * Client-generated: real candidate registration/dedup is out of scope for the thin
   * slice (see the grilled plan), so each public apply-form submission mints a fresh id
   * rather than looking up or creating a reusable Candidate identity.
   */
  candidateId: string;
  positionId: string;
  candidateName: string;
  candidateEmail: string;
  candidatePhone?: string;
  resumeUrl?: string;
}

export interface CandidatePipelineItem {
  candidateProcessId: string;
  candidateName: string;
  candidateEmail: string;
  status: PipelineStatus;
  currentStageId: string | null;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface CandidateNoteDetail {
  authorId: string;
  text: string;
  isPrivate: boolean;
  createdAt: string;
}

export interface CandidateEvaluationDetail {
  stageId: string;
  evaluatorId: string;
  score: number;
  comments: string | null;
  submittedAt: string;
}

export interface CandidateDetail {
  candidateProcessId: string;
  /** The raw candidate id — matches Conversation.candidateId in HrBot, used to look up a candidate's chat via getConversationForCandidate. */
  candidateId: string;
  positionId: string;
  candidateName: string;
  candidateEmail: string;
  candidatePhone: string | null;
  resumeUrl: string | null;
  status: PipelineStatus;
  currentStageId: string | null;
  notes: CandidateNoteDetail[];
  evaluations: CandidateEvaluationDetail[];
}

export interface SubmitEvaluationRequest {
  evaluatorId: string;
  score: number;
  comments?: string;
}

// ---- HR Bot ---------------------------------------------------------------

export type ConversationInterface = "WebChat" | "FacebookMessenger";
export type ConversationStatus = "InProgress" | "Completed" | "ScreenedOut" | "TimedOut";

export type StepType =
  | "SendMessage"
  | "SendQuickReply"
  | "WaitResponse"
  | "SendImage"
  | "SendVideo"
  | "SendEmail"
  | "SwitchFlow"
  | "FaqEngine"
  | "CallWebHook";

/** What's needed to render the step a conversation is currently sitting on. */
export interface ConversationStepContent {
  stepId: string;
  type: StepType;
  content: string;
  quickReplyOptions: string[];
  isFinalStep: boolean;
}

/** Keyed by positionId, not botProjectId — the candidate app has no reason to know a BotProject exists as its own concept. */
export interface StartConversationRequest {
  positionId: string;
  candidateId: string;
  interface: ConversationInterface;
}

export interface StartConversationResult {
  conversationId: string;
  currentStep: ConversationStepContent;
}

/** See ADR 0004: candidateToken must be presented (as the X-Candidate-Token header) on every later request for this conversation. */
export interface StartConversationResponse {
  conversation: StartConversationResult;
  candidateToken: string;
}

export interface ProcessUserResponseResult {
  status: ConversationStatus;
  /** Null once the conversation has ended — there's no further step to render. */
  currentStep: ConversationStepContent | null;
  routeMatched: boolean;
  faqAnswer: string | null;
}

export interface ConversationLogEntry {
  stepId: string;
  /** The bot's question at this step, resolved from the project's flow — null if that step no longer exists (e.g. the flow was edited after this conversation happened). */
  botMessage: string | null;
  candidateResponse: string | null;
  loggedAt: string;
}

export interface ConversationVariableEntry {
  key: string;
  value: string;
}

export interface ConversationHistory {
  conversationId: string;
  status: ConversationStatus;
  startedAt: string;
  completedAt: string | null;
  logs: ConversationLogEntry[];
  variables: ConversationVariableEntry[];
}

/** null means the candidate hasn't started an HR chat for this position yet — not an error. */
export interface ConversationForCandidateResponse {
  conversationId: string | null;
}

// ---- Video Interview --------------------------------------------------------

export type CaseStepType =
  | "ShowMessage"
  | "PlayVideoQuestion"
  | "RecordVideoAnswer"
  | "UploadDocument"
  | "TakeNote"
  | "AddCalendarEvent"
  | "SetPoint"
  | "QuickReply"
  | "FillInTheBlank"
  | "BasketQuestion"
  | "SoftwareDevelopmentQuestion";

/** Keyed by positionId, not caseBotProjectId — same reasoning as HrBot's StartConversationRequest. */
export interface StartCandidateCaseRequest {
  positionId: string;
  candidateId: string;
}

export interface StartCandidateCaseResult {
  caseId: string;
  retakesAllowed: number;
  stepId: string;
  stepType: CaseStepType;
  content: string;
  preparationTimeSeconds: number | null;
  recordingTimeSeconds: number | null;
}

/** See ADR 0004: candidateToken must be presented (as the X-Candidate-Token header) on every later request for this case. */
export interface StartCandidateCaseResponse {
  case: StartCandidateCaseResult;
  candidateToken: string;
}

export interface CodeReviewResult {
  readability: number;
  functionality: number;
  dataValidation: number;
  useCaseHandling: number;
  syntax: number;
  total: number;
}

export interface ReportSection {
  title: string;
  content: string;
}

export interface CaseReport {
  caseId: string;
  generatedAt: string;
  sections: ReportSection[];
}

/** caseId is null when the candidate hasn't started their video interview yet — not an error. */
export interface CaseForCandidateResponse {
  caseId: string | null;
}

export interface ScorableStep {
  stepId: string;
  content: string;
  relatedCompetencyIds: string[];
}

export interface Competency {
  id: string;
  name: string;
}

export interface ScoringContext {
  steps: ScorableStep[];
  competencies: Competency[];
}

export interface CandidateComparisonItem {
  caseId: string;
  candidateId: string;
  overallScore: number;
  competencyScores: Record<string, number>;
}

export interface CandidateNameLookup {
  candidateProcessId: string;
  name: string;
}

export interface CaseBotProjectSummary {
  id: string;
  name: string;
  retakesAllowed: number;
  retentionPeriodDays: number;
}

export interface CreateCaseBotProjectRequest {
  name: string;
  positionId: string;
  retakesAllowed: number;
  retentionPeriodDays: number;
}

export interface BotProjectSummary {
  id: string;
  name: string;
  retentionPeriodDays: number;
}

export interface CreateBotProjectRequest {
  name: string;
  positionId: string;
  retentionPeriodDays: number;
}
