import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useTranslation } from "react-i18next";
import { Button, Card, CardHeader, CardTitle, Input, Select, useToast } from "@peoplise/ui";
import { toApiError } from "@peoplise/api-client";
import { useCaseForCandidate, useScoringContext, useSubmitReviewerScoring } from "../hooks/useCases";

interface ReviewerScoringCardProps {
  candidateId: string;
  positionId: string;
}

const submitScoringSchema = z.object({
  stepId: z.string().min(1),
  competencyId: z.string().min(1),
  score: z.coerce.number().int().min(0).max(100),
  notes: z.string().optional(),
});
type SubmitScoringForm = z.infer<typeof submitScoringSchema>;

/**
 * A step's RelatedCompetencyIds narrows which competencies make sense to score it
 * against — falls back to every project competency when a step has none set, so
 * scoring is never blocked by missing authoring data.
 */
export function ReviewerScoringCard({ candidateId, positionId }: ReviewerScoringCardProps) {
  const { t } = useTranslation();
  const { show } = useToast();
  const lookup = useCaseForCandidate(candidateId, positionId);
  const caseId = lookup.data?.caseId ?? null;
  const context = useScoringContext(caseId);
  const submitScoring = useSubmitReviewerScoring(caseId);

  const {
    register,
    handleSubmit,
    watch,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<SubmitScoringForm>({
    resolver: zodResolver(submitScoringSchema),
    defaultValues: { score: 0 },
  });

  const selectedStepId = watch("stepId");
  const selectedStep = context.data?.steps.find((step) => step.stepId === selectedStepId);
  const availableCompetencies =
    selectedStep && selectedStep.relatedCompetencyIds.length > 0
      ? context.data?.competencies.filter((c) => selectedStep.relatedCompetencyIds.includes(c.id))
      : context.data?.competencies;

  const onSubmit = handleSubmit(async (values) => {
    try {
      await submitScoring.mutateAsync(values);
      show("Score submitted.", "success");
      reset({ stepId: "", competencyId: "", score: 0, notes: "" });
    } catch (error) {
      show(toApiError(error).title, "error");
    }
  });

  return (
    <Card>
      <CardHeader>
        <CardTitle>{t("candidateDetail.reviewerScoring")}</CardTitle>
      </CardHeader>

      {(lookup.isLoading || (caseId && context.isLoading)) && <p className="text-sm text-slate-500">{t("common.loading")}</p>}

      {!lookup.isLoading && !caseId && <p className="text-sm text-slate-400">{t("candidateDetail.noCase")}</p>}

      {context.data && context.data.steps.length === 0 && (
        <p className="text-sm text-slate-400">{t("candidateDetail.noStepsToScore")}</p>
      )}

      {context.data && context.data.steps.length > 0 && (
        <form onSubmit={onSubmit} className="flex flex-col gap-3" noValidate>
          <Select label={t("candidateDetail.step") as string} error={errors.stepId?.message} {...register("stepId")}>
            <option value="">—</option>
            {context.data.steps.map((step) => (
              <option key={step.stepId} value={step.stepId}>
                {step.content}
              </option>
            ))}
          </Select>

          <Select
            label={t("candidateDetail.competency") as string}
            error={errors.competencyId?.message}
            {...register("competencyId")}
          >
            <option value="">—</option>
            {availableCompetencies?.map((competency) => (
              <option key={competency.id} value={competency.id}>
                {competency.name}
              </option>
            ))}
          </Select>

          <Input
            label={t("candidateDetail.score") as string}
            type="number"
            min={0}
            max={100}
            error={errors.score?.message}
            {...register("score")}
          />

          <Input label={t("candidateDetail.scoringNotes") as string} error={errors.notes?.message} {...register("notes")} />

          <Button type="submit" disabled={isSubmitting} className="w-fit">
            {t("common.submit")}
          </Button>
        </form>
      )}
    </Card>
  );
}
