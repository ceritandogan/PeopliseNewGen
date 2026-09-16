import { useMutation } from "@tanstack/react-query";
import { candidatesApi, type SubmitCandidateApplicationRequest } from "@peoplise/api-client";

export function useSubmitApplication() {
  return useMutation({
    mutationFn: (request: SubmitCandidateApplicationRequest) => candidatesApi.submitApplication(request),
  });
}
