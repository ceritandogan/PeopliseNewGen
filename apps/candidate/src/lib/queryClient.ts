import { QueryClient } from "@tanstack/react-query";
import { toApiError } from "@peoplise/api-client";

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: (failureCount, error) => toApiError(error).status !== 404 && failureCount < 2,
      staleTime: 30_000,
    },
  },
});
