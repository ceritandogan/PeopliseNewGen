import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useTranslation } from "react-i18next";
import { Button, Card, CardHeader, CardTitle, Input, useToast } from "@peoplise/ui";
import { toApiError, type CodeReviewResult } from "@peoplise/api-client";
import { useCaseForCandidate, useRequestAICodeReview } from "../hooks/useCases";

interface AICodeReviewCardProps {
  candidateId: string;
  positionId: string;
}

const requestReviewSchema = z.object({
  question: z.string().min(1),
  candidateCode: z.string().min(1),
});
type RequestReviewForm = z.infer<typeof requestReviewSchema>;

const DIMENSION_KEYS = ["readability", "functionality", "dataValidation", "useCaseHandling", "syntax"] as const;

export function AICodeReviewCard({ candidateId, positionId }: AICodeReviewCardProps) {
  const { t } = useTranslation();
  const { show } = useToast();
  const lookup = useCaseForCandidate(candidateId, positionId);
  const caseId = lookup.data?.caseId ?? null;
  const requestReview = useRequestAICodeReview(caseId);
  const [result, setResult] = useState<CodeReviewResult | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<RequestReviewForm>({ resolver: zodResolver(requestReviewSchema) });

  const onSubmit = handleSubmit(async (values) => {
    try {
      const review = await requestReview.mutateAsync(values);
      setResult(review);
    } catch (error) {
      show(toApiError(error).title, "error");
    }
  });

  return (
    <Card>
      <CardHeader>
        <CardTitle>{t("candidateDetail.aiCodeReview")}</CardTitle>
      </CardHeader>

      {lookup.isLoading && <p className="text-sm text-slate-500">{t("common.loading")}</p>}

      {!lookup.isLoading && !caseId && <p className="text-sm text-slate-400">{t("candidateDetail.noCase")}</p>}

      {caseId && (
        <form onSubmit={onSubmit} className="flex flex-col gap-3" noValidate>
          <Input label={t("candidateDetail.codeReviewQuestion") as string} error={errors.question?.message} {...register("question")} />

          <div className="flex flex-col gap-1.5">
            <label htmlFor="candidate-code" className="text-sm font-medium text-slate-700">
              {t("candidateDetail.candidateCode")}
            </label>
            <textarea
              id="candidate-code"
              rows={8}
              className="rounded-md border border-slate-300 bg-white px-3 py-2 font-mono text-sm text-slate-900 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500"
              {...register("candidateCode")}
            />
            {errors.candidateCode && <p className="text-sm text-red-600">{errors.candidateCode.message}</p>}
          </div>

          <Button type="submit" disabled={isSubmitting} className="w-fit">
            {t("candidateDetail.requestReview")}
          </Button>
        </form>
      )}

      {result && (
        <dl className="mt-4 grid grid-cols-2 gap-2 border-t border-slate-100 pt-4 text-sm">
          {DIMENSION_KEYS.map((key) => (
            <div key={key} className="contents">
              <dt className="text-slate-500">{t(`candidateDetail.codeReview.${key}`)}</dt>
              <dd className="text-slate-900">{result[key]}/20</dd>
            </div>
          ))}
          <dt className="font-medium text-slate-700">{t("candidateDetail.codeReview.total")}</dt>
          <dd className="font-medium text-slate-900">{result.total}/100</dd>
        </dl>
      )}
    </Card>
  );
}
