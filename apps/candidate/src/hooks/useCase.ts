import { useMutation } from "@tanstack/react-query";
import { casesApi, type StartCandidateCaseRequest } from "@peoplise/api-client";

export function useStartCandidateCase() {
  return useMutation({
    mutationFn: (request: StartCandidateCaseRequest) => casesApi.startCandidateCase(request),
  });
}

export function useSubmitVideoAnswer(caseId: string) {
  return useMutation({
    mutationFn: ({ stepId, file }: { stepId: string; file: File }) =>
      casesApi.submitVideoAnswer(caseId, stepId, file),
  });
}
