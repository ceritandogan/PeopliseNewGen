import { useQuery } from "@tanstack/react-query";
import { casesApi } from "@peoplise/api-client";

export function useCaseForCandidate(candidateId: string | undefined, positionId: string | undefined) {
  return useQuery({
    queryKey: ["cases", "for-candidate", candidateId, positionId],
    queryFn: () => casesApi.getCaseForCandidate(candidateId!, positionId!),
    enabled: Boolean(candidateId && positionId),
  });
}

export function useCaseReport(caseId: string | null | undefined) {
  return useQuery({
    queryKey: ["cases", caseId, "report"],
    queryFn: () => casesApi.getCaseReport(caseId!),
    enabled: Boolean(caseId),
    retry: false,
  });
}
