/**
 * Hand-typed to mirror the backend's actual Application-layer DTOs (Peoplise.Modules.*).
 * Regenerate this file (and the resource modules) with `openapi-typescript` (or similar)
 * once real HTTP controllers exist and a live `swagger.json` can be fetched — as of
 * Stage 5, no module has HTTP endpoints yet, only MediatR command/query handlers, so
 * there is no OpenAPI spec to generate *from* today. Keep these in sync by hand until then.
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

export interface StartConversationRequest {
  botProjectId: string;
  candidateId: string;
  interface: ConversationInterface;
}

export interface ProcessUserResponseResult {
  status: ConversationStatus;
  nextStepId: string | null;
  routeMatched: boolean;
  faqAnswer: string | null;
}

export interface ConversationLogEntry {
  stepId: string;
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

// ---- Video Interview --------------------------------------------------------

export interface StartCandidateCaseRequest {
  caseBotProjectId: string;
  candidateId: string;
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

export interface CandidateComparisonItem {
  caseId: string;
  candidateId: string;
  overallScore: number;
  competencyScores: Record<string, number>;
}
