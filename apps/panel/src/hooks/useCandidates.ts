import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  candidatesApi,
  type AddCandidateManuallyRequest,
  type CandidatePipelineItem,
  type GetCandidatePipelineParams,
  type PagedResult,
  type PipelineStatus,
  type SubmitEvaluationRequest,
} from "@peoplise/api-client";

export function useCandidatePipeline(params: GetCandidatePipelineParams) {
  return useQuery({
    queryKey: ["candidates", "pipeline", params],
    queryFn: () => candidatesApi.getCandidatePipeline(params),
    enabled: Boolean(params.positionId),
  });
}

/**
 * The Kanban board's drag-and-drop move. Optimistic: the card needs to relocate the
 * instant the drag ends, not after a round trip — `setQueriesData` (plural) patches every
 * cached pipeline query, not just the one shape this page happens to fetch with, and
 * `onError` restores exactly what was there before if the request fails.
 */
export function useSetCandidateStatus() {
  const queryClient = useQueryClient();
  const pipelineKey = ["candidates", "pipeline"];

  return useMutation({
    mutationFn: ({ candidateProcessId, status }: { candidateProcessId: string; status: PipelineStatus }) =>
      candidatesApi.setCandidateStatus(candidateProcessId, status),
    onMutate: async ({ candidateProcessId, status }) => {
      await queryClient.cancelQueries({ queryKey: pipelineKey });
      const previous = queryClient.getQueriesData<PagedResult<CandidatePipelineItem>>({ queryKey: pipelineKey });

      queryClient.setQueriesData<PagedResult<CandidatePipelineItem>>({ queryKey: pipelineKey }, (old) =>
        old
          ? { ...old, items: old.items.map((item) => (item.candidateProcessId === candidateProcessId ? { ...item, status } : item)) }
          : old,
      );

      return { previous };
    },
    onError: (_error, _variables, context) => {
      context?.previous.forEach(([queryKey, data]) => queryClient.setQueryData(queryKey, data));
    },
    onSettled: () => queryClient.invalidateQueries({ queryKey: pipelineKey }),
  });
}

export function useAddCandidateManually(positionId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: Omit<AddCandidateManuallyRequest, "positionId">) =>
      candidatesApi.addCandidateManually({ ...request, positionId }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["candidates", "pipeline"] }),
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
    mutationFn: (request: { text: string; isPrivate: boolean }) =>
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
