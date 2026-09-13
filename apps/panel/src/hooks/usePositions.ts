import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { type CreatePositionRequest, positionsApi } from "@peoplise/api-client";

export function usePositionDashboard(positionId: string | undefined) {
  return useQuery({
    queryKey: ["positions", positionId, "dashboard"],
    queryFn: () => positionsApi.getPositionDashboard(positionId!),
    enabled: Boolean(positionId),
  });
}

export function useCreatePosition() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: CreatePositionRequest) => positionsApi.createPosition(request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["positions"] }),
  });
}
