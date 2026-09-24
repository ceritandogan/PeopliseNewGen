import { httpClient } from "../http";
import type { StageRuleType, StageType, WorkflowStagesResult } from "../types";

/** A position points at its workflow, not the reverse — see the backend query's own remarks. */
export async function getWorkflowStagesForPosition(positionId: string): Promise<WorkflowStagesResult> {
  const { data } = await httpClient.get<WorkflowStagesResult>("/api/workflows", { params: { positionId } });
  return data;
}

/** Order is computed by the caller (append to the end of the already-loaded stage list) — see the backend request DTO's own remarks. */
export async function addWorkflowStage(
  workflowDefinitionId: string,
  request: { name: string; type: StageType; order: number },
): Promise<string> {
  const { data } = await httpClient.post<string>(`/api/workflows/${workflowDefinitionId}/stages`, request);
  return data;
}

/** Full-sequence replacement, not an incremental move — pass every stage id in its new order. */
export async function reorderWorkflowStages(workflowDefinitionId: string, orderedStageIds: string[]): Promise<void> {
  await httpClient.put(`/api/workflows/${workflowDefinitionId}/stages/reorder`, { orderedStageIds });
}

export async function removeWorkflowStage(workflowDefinitionId: string, stageId: string): Promise<void> {
  await httpClient.delete(`/api/workflows/${workflowDefinitionId}/stages/${stageId}`);
}

/** Threshold is required for AdvanceIfScoreAtLeast/EliminateIfScoreBelow, delayDays for ActivateAfterDelay — see the backend validator's own remarks. */
export async function addStageRule(
  workflowDefinitionId: string,
  stageId: string,
  request: { type: StageRuleType; threshold: number | null; delayDays: number | null },
): Promise<string> {
  const { data } = await httpClient.post<string>(`/api/workflows/${workflowDefinitionId}/stages/${stageId}/rules`, request);
  return data;
}

export async function removeStageRule(workflowDefinitionId: string, stageId: string, ruleId: string): Promise<void> {
  await httpClient.delete(`/api/workflows/${workflowDefinitionId}/stages/${stageId}/rules/${ruleId}`);
}
