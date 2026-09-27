import { httpClient } from "../http";
import type { CreateTenantRequest, CreateTenantResponse, CurrentTenant } from "../types";

export async function createTenant(request: CreateTenantRequest): Promise<CreateTenantResponse> {
  const { data } = await httpClient.post<CreateTenantResponse>("/api/tenants", request);
  return data;
}

export async function getCurrentTenant(): Promise<CurrentTenant> {
  const { data } = await httpClient.get<CurrentTenant>("/api/tenants/current");
  return data;
}
