import { httpClient } from "../http";
import type {
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
