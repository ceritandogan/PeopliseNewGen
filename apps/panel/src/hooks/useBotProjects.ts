import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { botProjectsApi, type AddBotStepRequest, type AddBotStepRouteRequest, type CreateBotProjectRequest } from "@peoplise/api-client";

export function useBotProjectsForPosition(positionId: string | undefined) {
  return useQuery({
    queryKey: ["positions", positionId, "bot-projects"],
    queryFn: () => botProjectsApi.getBotProjectsForPosition(positionId!),
    enabled: Boolean(positionId),
  });
}

export function useCreateBotProject(positionId: string | undefined) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: CreateBotProjectRequest) => botProjectsApi.createBotProject(request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["positions", positionId, "bot-projects"] }),
  });
}

function botFlowsQueryKey(botProjectId: string | undefined) {
  return ["bot-projects", botProjectId, "flows"];
}

export function useFlowsForBotProject(botProjectId: string | undefined) {
  return useQuery({
    queryKey: botFlowsQueryKey(botProjectId),
    queryFn: () => botProjectsApi.getFlowsForBotProject(botProjectId!),
    enabled: Boolean(botProjectId),
  });
}

export function useAddBotFlow(botProjectId: string | undefined) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: { name: string; isDefault: boolean }) => botProjectsApi.addBotFlow(botProjectId!, request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: botFlowsQueryKey(botProjectId) }),
  });
}

export function useAddBotStep(botProjectId: string | undefined) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ flowId, ...request }: AddBotStepRequest & { flowId: string }) =>
      botProjectsApi.addBotStep(botProjectId!, flowId, request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: botFlowsQueryKey(botProjectId) }),
  });
}

export function useAddBotStepRoute(botProjectId: string | undefined) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ flowId, stepId, ...request }: AddBotStepRouteRequest & { flowId: string; stepId: string }) =>
      botProjectsApi.addBotStepRoute(botProjectId!, flowId, stepId, request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: botFlowsQueryKey(botProjectId) }),
  });
}

export function useRemoveBotStepRoute(botProjectId: string | undefined) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ flowId, stepId, routeId }: { flowId: string; stepId: string; routeId: string }) =>
      botProjectsApi.removeBotStepRoute(botProjectId!, flowId, stepId, routeId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: botFlowsQueryKey(botProjectId) }),
  });
}
