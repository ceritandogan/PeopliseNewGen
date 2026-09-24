import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { casesApi, type AddFlowStepRequest } from "@peoplise/api-client";

function flowsQueryKey(caseBotProjectId: string | undefined) {
  return ["case-bot-projects", caseBotProjectId, "flows"];
}

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

export function useScoringContext(caseId: string | null | undefined) {
  return useQuery({
    queryKey: ["cases", caseId, "scoring-context"],
    queryFn: () => casesApi.getScoringContext(caseId!),
    enabled: Boolean(caseId),
  });
}

export function useCandidateComparison(caseBotProjectId: string | undefined) {
  return useQuery({
    queryKey: ["case-bot-projects", caseBotProjectId, "comparison"],
    queryFn: () => casesApi.getCandidateComparison(caseBotProjectId!),
    enabled: Boolean(caseBotProjectId),
  });
}

export function useCompetenciesForProject(caseBotProjectId: string | undefined) {
  return useQuery({
    queryKey: ["case-bot-projects", caseBotProjectId, "competencies"],
    queryFn: () => casesApi.getCompetenciesForProject(caseBotProjectId!),
    enabled: Boolean(caseBotProjectId),
  });
}

export function useSubmitReviewerScoring(caseId: string | null | undefined) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: { stepId: string; competencyId: string; score: number; notes?: string }) =>
      casesApi.submitReviewerScoring(caseId!, request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["cases", caseId, "report"] }),
  });
}

/** stepId is minted by the caller — no candidate-facing "SoftwareDevelopmentQuestion" step exists yet, see CasesController.RequestCodeReview's remarks. */
export function useRequestAICodeReview(caseId: string | null | undefined) {
  return useMutation({
    mutationFn: (request: { question: string; candidateCode: string }) =>
      casesApi.requestAICodeReview(caseId!, { stepId: crypto.randomUUID(), ...request }),
  });
}

export function useFlowsForProject(caseBotProjectId: string | undefined) {
  return useQuery({
    queryKey: flowsQueryKey(caseBotProjectId),
    queryFn: () => casesApi.getFlowsForProject(caseBotProjectId!),
    enabled: Boolean(caseBotProjectId),
  });
}

export function useAddFlow(caseBotProjectId: string | undefined) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: { name: string; isDefault: boolean }) => casesApi.addFlow(caseBotProjectId!, request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: flowsQueryKey(caseBotProjectId) }),
  });
}

export function useAddFlowStep(caseBotProjectId: string | undefined) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ flowId, ...request }: AddFlowStepRequest & { flowId: string }) =>
      casesApi.addFlowStep(caseBotProjectId!, flowId, request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: flowsQueryKey(caseBotProjectId) }),
  });
}
