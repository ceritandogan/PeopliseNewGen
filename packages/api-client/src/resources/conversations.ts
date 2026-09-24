import { httpClient } from "../http";
import type {
  ConversationForCandidateResponse,
  ConversationHistory,
  ProcessUserResponseResult,
  StartConversationRequest,
  StartConversationResponse,
} from "../types";

export async function startConversation(request: StartConversationRequest): Promise<StartConversationResponse> {
  const { data } = await httpClient.post<StartConversationResponse>("/api/conversations", request);
  return data;
}

/**
 * candidateToken is the value returned from startConversation — see ADR 0004. Sent as
 * X-Candidate-Token, proving this caller is the one this conversation was started for.
 */
export async function sendConversationResponse(
  conversationId: string,
  response: string | null,
  candidateToken: string,
): Promise<ProcessUserResponseResult> {
  const { data } = await httpClient.post<ProcessUserResponseResult>(
    `/api/conversations/${conversationId}/responses`,
    { response },
    { headers: { "X-Candidate-Token": candidateToken } },
  );
  return data;
}

export async function getConversationHistory(conversationId: string): Promise<ConversationHistory> {
  const { data } = await httpClient.get<ConversationHistory>(`/api/conversations/${conversationId}/history`);
  return data;
}

/** HR/panel-only lookup — finds the conversation (if any) a candidate has for one position, given what CandidateDetailPage already has. */
export async function getConversationForCandidate(
  candidateId: string,
  positionId: string,
): Promise<ConversationForCandidateResponse> {
  const { data } = await httpClient.get<ConversationForCandidateResponse>("/api/conversations/by-candidate", {
    params: { candidateId, positionId },
  });
  return data;
}

/** HR/panel-only: re-emails the candidate's bot-chat link. Unlike Start's delivery, a failure here surfaces as a real error. */
export async function resendConversationLink(candidateId: string, positionId: string): Promise<void> {
  await httpClient.post("/api/conversations/resend-link", { candidateId, positionId });
}
