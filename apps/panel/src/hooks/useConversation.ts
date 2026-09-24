import { useMutation, useQuery } from "@tanstack/react-query";
import { conversationsApi } from "@peoplise/api-client";

export function useConversationForCandidate(candidateId: string | undefined, positionId: string | undefined) {
  return useQuery({
    queryKey: ["conversations", "for-candidate", candidateId, positionId],
    queryFn: () => conversationsApi.getConversationForCandidate(candidateId!, positionId!),
    enabled: Boolean(candidateId && positionId),
  });
}

export function useResendConversationLink() {
  return useMutation({
    mutationFn: ({ candidateId, positionId }: { candidateId: string; positionId: string }) =>
      conversationsApi.resendConversationLink(candidateId, positionId),
  });
}

export function useConversationHistory(conversationId: string | null | undefined) {
  return useQuery({
    queryKey: ["conversations", conversationId, "history"],
    queryFn: () => conversationsApi.getConversationHistory(conversationId!),
    enabled: Boolean(conversationId),
  });
}
