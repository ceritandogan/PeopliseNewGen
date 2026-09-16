import { httpClient } from "../http";
import type { CreatePositionRequest, PagedResult, PositionDashboard, PositionListItem } from "../types";

export async function createPosition(request: CreatePositionRequest): Promise<{ id: string }> {
  const { data } = await httpClient.post<{ id: string }>("/api/positions", request);
  return data;
}

export interface GetPositionsListParams {
  page?: number;
  pageSize?: number;
}

export async function getPositionsList({
  page = 1,
  pageSize = 20,
}: GetPositionsListParams = {}): Promise<PagedResult<PositionListItem>> {
  const { data } = await httpClient.get<PagedResult<PositionListItem>>("/api/positions", {
    params: { page, pageSize },
  });
  return data;
}

export async function getPositionDashboard(positionId: string): Promise<PositionDashboard> {
  const { data } = await httpClient.get<PositionDashboard>(`/api/positions/${positionId}/dashboard`);
  return data;
}
