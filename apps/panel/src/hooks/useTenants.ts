import { useMutation, useQuery } from "@tanstack/react-query";
import { type CreateTenantRequest, tenantsApi } from "@peoplise/api-client";

export function useCurrentTenant() {
  return useQuery({
    queryKey: ["tenants", "current"],
    queryFn: () => tenantsApi.getCurrentTenant(),
  });
}

export function useCreateTenant() {
  return useMutation({
    mutationFn: (request: CreateTenantRequest) => tenantsApi.createTenant(request),
  });
}
