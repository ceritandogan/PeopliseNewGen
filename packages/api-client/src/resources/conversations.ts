import { httpClient } from "../http";
import type {
  ConversationHistory,
  ProcessUserResponseResult,
  StartConversationRequest,
  StartConversationResult,
} from "../types";

export async function startConversation(request: StartConversationRequest): Promise<StartConversationResult> {
  const { data } = await httpClient.post<StartConversationResult>("/api/conversations", request);
  return data;
}

export async function sendConversationResponse(
  conversationId: string,
  response: string | null,
): Promise<ProcessUserResponseResult> {
  const { data } = await httpClient.post<ProcessUserResponseResult>(
    `/api/conversations/${conversationId}/responses`,
    { response },
  );
  return data;
}

export async function getConversationHistory(conversationId: string): Promise<ConversationHistory> {
  const { data } = await httpClient.get<ConversationHistory>(`/api/conversations/${conversationId}/history`);
  return data;
}
