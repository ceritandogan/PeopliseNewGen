import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { botProjectsApi, type CreateBotProjectRequest } from "@peoplise/api-client";

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
