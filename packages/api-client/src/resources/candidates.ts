import { httpClient } from "../http";
import type {
  CandidateDetail,
  CandidatePipelineItem,
  PagedResult,
  PipelineStatus,
  SubmitEvaluationRequest,
} from "../types";

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

/**
 * TODO(backend): the Candidate Detail page needs this ("not ekleme" in the architecture
 * doc), but ATS's Application layer only ever got the 5 commands the doc's Stage 2 spec
 * named — there's no AddCandidateNoteCommand yet, even though CandidateProcess.AddNote
 * exists at the domain level. This call has nothing to reach until that command (and a
 * controller route for it) is added.
 */
export async function addCandidateNote(
  candidateProcessId: string,
  request: { authorId: string; text: string; isPrivate: boolean },
): Promise<void> {
  await httpClient.post(`/api/candidates/${candidateProcessId}/notes`, request);
}

export async function transitionCandidateStage(candidateProcessId: string): Promise<void> {
  await httpClient.post(`/api/candidates/${candidateProcessId}/transition`, {});
}
