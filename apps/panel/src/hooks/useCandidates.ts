import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { candidatesApi, type GetCandidatePipelineParams, type SubmitEvaluationRequest } from "@peoplise/api-client";

export function useCandidatePipeline(params: GetCandidatePipelineParams) {
  return useQuery({
    queryKey: ["candidates", "pipeline", params],
    queryFn: () => candidatesApi.getCandidatePipeline(params),
    enabled: Boolean(params.positionId),
  });
}

export function useCandidateDetail(candidateProcessId: string | undefined) {
  return useQuery({
    queryKey: ["candidates", candidateProcessId],
    queryFn: () => candidatesApi.getCandidateDetail(candidateProcessId!),
    enabled: Boolean(candidateProcessId),
  });
}

export function useSubmitEvaluation(candidateProcessId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: SubmitEvaluationRequest) => candidatesApi.submitEvaluation(candidateProcessId, request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["candidates", candidateProcessId] }),
  });
}

export function useAddCandidateNote(candidateProcessId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: { authorId: string; text: string; isPrivate: boolean }) =>
      candidatesApi.addCandidateNote(candidateProcessId, request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["candidates", candidateProcessId] }),
  });
}

export function useCandidateNames(positionId: string | undefined, candidateIds: string[]) {
  return useQuery({
    queryKey: ["candidates", "names", positionId, candidateIds],
    queryFn: () => candidatesApi.getCandidateNames(positionId!, candidateIds),
    enabled: Boolean(positionId) && candidateIds.length > 0,
  });
}
