import { httpClient } from "../http";
import type {
  AddBotStepRequest,
  AddBotStepRouteRequest,
  BotFlow,
  BotProjectSummary,
  CreateBotProjectRequest,
  ProjectVariable,
} from "../types";

export async function createBotProject(request: CreateBotProjectRequest): Promise<string> {
  const { data } = await httpClient.post<string>("/api/bot-projects", request);
  return data;
}

export async function getBotProjectsForPosition(positionId: string): Promise<BotProjectSummary[]> {
  const { data } = await httpClient.get<BotProjectSummary[]>("/api/bot-projects", { params: { positionId } });
  return data;
}

export async function getFlowsForBotProject(botProjectId: string): Promise<BotFlow[]> {
  const { data } = await httpClient.get<BotFlow[]>(`/api/bot-projects/${botProjectId}/flows`);
  return data;
}

export async function addBotFlow(botProjectId: string, request: { name: string; isDefault: boolean }): Promise<string> {
  const { data } = await httpClient.post<string>(`/api/bot-projects/${botProjectId}/flows`, request);
  return data;
}

export async function addBotStep(botProjectId: string, flowId: string, request: AddBotStepRequest): Promise<string> {
  const { data } = await httpClient.post<string>(`/api/bot-projects/${botProjectId}/flows/${flowId}/steps`, request);
  return data;
}

export async function addBotStepRoute(
  botProjectId: string,
  flowId: string,
  stepId: string,
  request: AddBotStepRouteRequest,
): Promise<string> {
  const { data } = await httpClient.post<string>(
    `/api/bot-projects/${botProjectId}/flows/${flowId}/steps/${stepId}/routes`,
    request,
  );
  return data;
}

export async function removeBotStepRoute(botProjectId: string, flowId: string, stepId: string, routeId: string): Promise<void> {
  await httpClient.delete(`/api/bot-projects/${botProjectId}/flows/${flowId}/steps/${stepId}/routes/${routeId}`);
}

export async function getVariablesForProject(botProjectId: string): Promise<ProjectVariable[]> {
  const { data } = await httpClient.get<ProjectVariable[]>(`/api/bot-projects/${botProjectId}/variables`);
  return data;
}

export async function addVariable(botProjectId: string, request: { key: string; description?: string }): Promise<string> {
  const { data } = await httpClient.post<string>(`/api/bot-projects/${botProjectId}/variables`, request);
  return data;
}
