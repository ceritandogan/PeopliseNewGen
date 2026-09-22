import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { casesApi, type CreateCaseBotProjectRequest } from "@peoplise/api-client";

export function useCaseBotProjectsForPosition(positionId: string | undefined) {
  return useQuery({
    queryKey: ["positions", positionId, "case-bot-projects"],
    queryFn: () => casesApi.getCaseBotProjectsForPosition(positionId!),
    enabled: Boolean(positionId),
  });
}

export function useCreateCaseBotProject(positionId: string | undefined) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: CreateCaseBotProjectRequest) => casesApi.createCaseBotProject(request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["positions", positionId, "case-bot-projects"] }),
  });
}
