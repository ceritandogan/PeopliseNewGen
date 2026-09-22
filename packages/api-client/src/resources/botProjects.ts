import { httpClient } from "../http";
import type { BotProjectSummary, CreateBotProjectRequest } from "../types";

export async function createBotProject(request: CreateBotProjectRequest): Promise<string> {
  const { data } = await httpClient.post<string>("/api/bot-projects", request);
  return data;
}

export async function getBotProjectsForPosition(positionId: string): Promise<BotProjectSummary[]> {
  const { data } = await httpClient.get<BotProjectSummary[]>("/api/bot-projects", { params: { positionId } });
  return data;
}
