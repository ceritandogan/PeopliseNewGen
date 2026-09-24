import { httpClient } from "../http";
import type {
  AddFlowStepRequest,
  CandidateComparisonItem,
  CaseBotProjectSummary,
  CaseFlow,
  CaseForCandidateResponse,
  CaseReport,
  CodeReviewResult,
  Competency,
  CreateCaseBotProjectRequest,
  ScoringContext,
  StartCandidateCaseRequest,
  StartCandidateCaseResponse,
} from "../types";

export async function startCandidateCase(request: StartCandidateCaseRequest): Promise<StartCandidateCaseResponse> {
  const { data } = await httpClient.post<StartCandidateCaseResponse>("/api/cases", request);
  return data;
}

/**
 * candidateToken is the value returned from startCandidateCase — see ADR 0004. Sent as
 * X-Candidate-Token, proving this caller is the one this case was started for.
 */
export async function submitVideoAnswer(
  caseId: string,
  stepId: string,
  file: File,
  candidateToken: string,
  onUploadProgress?: (percent: number) => void,
): Promise<void> {
  const form = new FormData();
  form.append("stepId", stepId);
  form.append("video", file);

  await httpClient.post(`/api/cases/${caseId}/video-answers`, form, {
    headers: { "Content-Type": "multipart/form-data", "X-Candidate-Token": candidateToken },
    onUploadProgress: onUploadProgress
      ? (event) => onUploadProgress(event.total ? Math.round((event.loaded / event.total) * 100) : 0)
      : undefined,
  });
}

/** reviewerId is deliberately not a field here — the server derives it from the caller's access token, never from the request body. See CasesController.SubmitScoring. */
export async function submitReviewerScoring(
  caseId: string,
  request: { stepId: string; competencyId: string; score: number; notes?: string },
): Promise<void> {
  await httpClient.post(`/api/cases/${caseId}/scorings`, request);
}

export async function getScoringContext(caseId: string): Promise<ScoringContext> {
  const { data } = await httpClient.get<ScoringContext>(`/api/cases/${caseId}/scoring-context`);
  return data;
}

export async function requestAICodeReview(
  caseId: string,
  request: { stepId: string; question: string; candidateCode: string },
): Promise<CodeReviewResult> {
  const { data } = await httpClient.post<CodeReviewResult>(`/api/cases/${caseId}/code-review`, request);
  return data;
}

export async function getCaseReport(caseId: string): Promise<CaseReport> {
  const { data } = await httpClient.get<CaseReport>(`/api/cases/${caseId}/report`);
  return data;
}

/** HR/panel-only lookup — finds the case (if any) a candidate has for one position, given what CandidateDetailPage already has. */
export async function getCaseForCandidate(candidateId: string, positionId: string): Promise<CaseForCandidateResponse> {
  const { data } = await httpClient.get<CaseForCandidateResponse>("/api/cases/by-candidate", {
    params: { candidateId, positionId },
  });
  return data;
}

export async function getCandidateComparison(caseBotProjectId: string): Promise<CandidateComparisonItem[]> {
  const { data } = await httpClient.get<CandidateComparisonItem[]>(
    `/api/case-bot-projects/${caseBotProjectId}/comparison`,
  );
  return data;
}

export async function createCaseBotProject(request: CreateCaseBotProjectRequest): Promise<string> {
  const { data } = await httpClient.post<string>("/api/case-bot-projects", request);
  return data;
}

export async function getCaseBotProjectsForPosition(positionId: string): Promise<CaseBotProjectSummary[]> {
  const { data } = await httpClient.get<CaseBotProjectSummary[]>("/api/case-bot-projects", { params: { positionId } });
  return data;
}

/** Column headers (id+name) for the candidate comparison table — independent of any one case. */
export async function getCompetenciesForProject(caseBotProjectId: string): Promise<Competency[]> {
  const { data } = await httpClient.get<Competency[]>(`/api/case-bot-projects/${caseBotProjectId}/competencies`);
  return data;
}

export async function getFlowsForProject(caseBotProjectId: string): Promise<CaseFlow[]> {
  const { data } = await httpClient.get<CaseFlow[]>(`/api/case-bot-projects/${caseBotProjectId}/flows`);
  return data;
}

export async function addFlow(caseBotProjectId: string, request: { name: string; isDefault: boolean }): Promise<string> {
  const { data } = await httpClient.post<string>(`/api/case-bot-projects/${caseBotProjectId}/flows`, request);
  return data;
}

export async function addFlowStep(caseBotProjectId: string, flowId: string, request: AddFlowStepRequest): Promise<string> {
  const { data } = await httpClient.post<string>(`/api/case-bot-projects/${caseBotProjectId}/flows/${flowId}/steps`, request);
  return data;
}
