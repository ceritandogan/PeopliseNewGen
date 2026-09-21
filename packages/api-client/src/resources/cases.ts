import { httpClient } from "../http";
import type {
  CandidateComparisonItem,
  CaseReport,
  CodeReviewResult,
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

export async function submitReviewerScoring(
  caseId: string,
  request: { reviewerId: string; stepId: string; competencyId: string; score: number; notes?: string },
): Promise<void> {
  await httpClient.post(`/api/cases/${caseId}/scorings`, request);
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

export async function getCandidateComparison(caseBotProjectId: string): Promise<CandidateComparisonItem[]> {
  const { data } = await httpClient.get<CandidateComparisonItem[]>(
    `/api/case-bot-projects/${caseBotProjectId}/comparison`,
  );
  return data;
}
