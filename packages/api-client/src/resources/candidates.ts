import { httpClient } from "../http";
import type {
  CandidateDetail,
  CandidateNameLookup,
  CandidatePipelineItem,
  PagedResult,
  PipelineStatus,
  SubmitCandidateApplicationRequest,
  SubmitEvaluationRequest,
} from "../types";

/**
 * Anonymous by design — this is the public apply-form endpoint (`[AllowAnonymous]` on
 * the backend), called from the candidate app, which has no AuthProvider/session at
 * all. Goes through the same shared `httpClient` as everything else: its request
 * interceptor only *adds* an Authorization header when a session exists, so an
 * unauthenticated caller is unaffected.
 */
export async function submitApplication(request: SubmitCandidateApplicationRequest): Promise<{ id: string }> {
  const { data } = await httpClient.post<{ id: string }>("/api/candidates/apply", request);
  return data;
}

export interface GetCandidatePipelineParams {
  positionId: string;
  status?: PipelineStatus;
  page?: number;
  pageSize?: number;
}

export async function getCandidatePipeline({
  positionId,
  status,
  page = 1,
  pageSize = 20,
}: GetCandidatePipelineParams): Promise<PagedResult<CandidatePipelineItem>> {
  const { data } = await httpClient.get<PagedResult<CandidatePipelineItem>>(
    `/api/positions/${positionId}/candidates`,
    { params: { status, page, pageSize } },
  );
  return data;
}

export async function getCandidateDetail(candidateProcessId: string): Promise<CandidateDetail> {
  const { data } = await httpClient.get<CandidateDetail>(`/api/candidates/${candidateProcessId}`);
  return data;
}

export async function submitEvaluation(candidateProcessId: string, request: SubmitEvaluationRequest): Promise<void> {
  await httpClient.post(`/api/candidates/${candidateProcessId}/evaluations`, request);
}

/** No authorId here — deliberately: it's derived server-side from the caller's access token. See CandidatesController.AddNote. */
export async function addCandidateNote(
  candidateProcessId: string,
  request: { text: string; isPrivate: boolean },
): Promise<void> {
  await httpClient.post(`/api/candidates/${candidateProcessId}/notes`, request);
}

export async function transitionCandidateStage(candidateProcessId: string): Promise<void> {
  await httpClient.post(`/api/candidates/${candidateProcessId}/transition`, {});
}

/**
 * Bulk name lookup for raw candidate ids (as VideoInterview's Case.CandidateId carries them), scoped to one position.
 * An id with no matching CandidateProcess is simply omitted. Builds the query string by hand (repeated
 * `candidateIds=` keys) rather than passing `candidateIds` through axios's default `params` serializer, which
 * emits `candidateIds[]=` — a shape ASP.NET Core's `[FromQuery] Guid[]` binder silently binds to an empty array.
 */
export async function getCandidateNames(
  positionId: string,
  candidateIds: string[],
): Promise<Record<string, CandidateNameLookup>> {
  const query = new URLSearchParams({ positionId });
  for (const candidateId of candidateIds) query.append("candidateIds", candidateId);

  const { data } = await httpClient.get<Record<string, CandidateNameLookup>>(`/api/candidates/names?${query}`);
  return data;
}
