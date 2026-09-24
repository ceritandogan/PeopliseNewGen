import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { workflowsApi, type StageRuleType, type StageType } from "@peoplise/api-client";

function queryKey(positionId: string | undefined) {
  return ["positions", positionId, "workflow-stages"];
}

export function useWorkflowStages(positionId: string | undefined) {
  return useQuery({
    queryKey: queryKey(positionId),
    queryFn: () => workflowsApi.getWorkflowStagesForPosition(positionId!),
    enabled: Boolean(positionId),
  });
}

export function useAddWorkflowStage(positionId: string | undefined, workflowDefinitionId: string | undefined) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: { name: string; type: StageType; order: number }) =>
      workflowsApi.addWorkflowStage(workflowDefinitionId!, request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKey(positionId) }),
  });
}

export function useReorderWorkflowStages(positionId: string | undefined, workflowDefinitionId: string | undefined) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (orderedStageIds: string[]) => workflowsApi.reorderWorkflowStages(workflowDefinitionId!, orderedStageIds),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKey(positionId) }),
  });
}

export function useRemoveWorkflowStage(positionId: string | undefined, workflowDefinitionId: string | undefined) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (stageId: string) => workflowsApi.removeWorkflowStage(workflowDefinitionId!, stageId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKey(positionId) }),
  });
}

export function useAddStageRule(positionId: string | undefined, workflowDefinitionId: string | undefined) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      stageId,
      type,
      threshold,
      delayDays,
    }: {
      stageId: string;
      type: StageRuleType;
      threshold: number | null;
      delayDays: number | null;
    }) => workflowsApi.addStageRule(workflowDefinitionId!, stageId, { type, threshold, delayDays }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKey(positionId) }),
  });
}

export function useRemoveStageRule(positionId: string | undefined, workflowDefinitionId: string | undefined) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ stageId, ruleId }: { stageId: string; ruleId: string }) =>
      workflowsApi.removeStageRule(workflowDefinitionId!, stageId, ruleId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKey(positionId) }),
  });
}
