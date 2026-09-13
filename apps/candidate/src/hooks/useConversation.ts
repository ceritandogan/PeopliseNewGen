import { useMutation } from "@tanstack/react-query";
import { conversationsApi, type StartConversationRequest } from "@peoplise/api-client";

export function useStartConversation() {
  return useMutation({
    mutationFn: (request: StartConversationRequest) => conversationsApi.startConversation(request),
  });
}

export function useSendConversationResponse(conversationId: string) {
  return useMutation({
    mutationFn: (response: string | null) => conversationsApi.sendConversationResponse(conversationId, response),
  });
}
