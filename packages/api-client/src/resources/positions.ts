import { httpClient } from "../http";
import type { CreatePositionRequest, PositionDashboard } from "../types";

export async function createPosition(request: CreatePositionRequest): Promise<{ id: string }> {
  const { data } = await httpClient.post<{ id: string }>("/api/positions", request);
  return data;
}

export async function getPositionDashboard(positionId: string): Promise<PositionDashboard> {
  const { data } = await httpClient.get<PositionDashboard>(`/api/positions/${positionId}/dashboard`);
  return data;
}
