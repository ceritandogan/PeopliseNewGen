import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { type CreatePositionRequest, type GetPositionsListParams, positionsApi } from "@peoplise/api-client";

export function usePositionsList(params: GetPositionsListParams = {}) {
  return useQuery({
    queryKey: ["positions", "list", params],
    queryFn: () => positionsApi.getPositionsList(params),
  });
}

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
