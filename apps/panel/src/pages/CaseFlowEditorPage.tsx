import { useState } from "react";
import { Link, useParams } from "react-router";
import { useTranslation } from "react-i18next";
import { Button, Card, CardHeader, CardTitle, Input, Select, useToast } from "@peoplise/ui";
import { toApiError, type CaseFlow, type CaseStepType } from "@peoplise/api-client";
import { useAddFlow, useAddFlowStep, useCompetenciesForProject, useFlowsForProject } from "../hooks/useCases";

const CASE_STEP_TYPES: CaseStepType[] = [
  "ShowMessage",
  "PlayVideoQuestion",
  "RecordVideoAnswer",
  "UploadDocument",
  "TakeNote",
  "AddCalendarEvent",
  "SetPoint",
  "QuickReply",
  "FillInTheBlank",
  "BasketQuestion",
  "SoftwareDevelopmentQuestion",
];

/** Only these step types actually use a timer in the candidate app — see Step's own PreparationTimeSeconds/RecordingTimeSeconds doc comments. */
const TIMED_STEP_TYPES: CaseStepType[] = ["RecordVideoAnswer", "SoftwareDevelopmentQuestion"];

function AddFlowStepForm({
  flow,
  caseBotProjectId,
}: {
  flow: CaseFlow;
  caseBotProjectId: string;
}) {
  const { t } = useTranslation();
  const { show } = useToast();
  const { data: competencies } = useCompetenciesForProject(caseBotProjectId);
  const addFlowStep = useAddFlowStep(caseBotProjectId);

  const nextOrder = flow.steps.length > 0 ? Math.max(...flow.steps.map((s) => s.order)) + 1 : 0;
  const [type, setType] = useState<CaseStepType>("RecordVideoAnswer");
  const [content, setContent] = useState("");
  const [preparationTimeSeconds, setPreparationTimeSeconds] = useState("");
  const [recordingTimeSeconds, setRecordingTimeSeconds] = useState("");
  const [relatedCompetencyIds, setRelatedCompetencyIds] = useState<string[]>([]);
  const isTimed = TIMED_STEP_TYPES.includes(type);

  const toggleCompetency = (id: string) => {
    setRelatedCompetencyIds((current) => (current.includes(id) ? current.filter((c) => c !== id) : [...current, id]));
  };

  const onSubmit = async () => {
    if (!content.trim()) return;
    try {
      await addFlowStep.mutateAsync({
        flowId: flow.id,
        type,
        content,
        order: nextOrder,
        preparationTimeSeconds: isTimed && preparationTimeSeconds ? Number(preparationTimeSeconds) : null,
        recordingTimeSeconds: isTimed && recordingTimeSeconds ? Number(recordingTimeSeconds) : null,
        relatedCompetencyIds: relatedCompetencyIds.length > 0 ? relatedCompetencyIds : null,
      });
      setContent("");
      setPreparationTimeSeconds("");
      setRecordingTimeSeconds("");
      setRelatedCompetencyIds([]);
      show(t("positionDetail.stepAdded") as string, "success");
    } catch (error) {
      show(toApiError(error).title, "error");
    }
  };

  return (
    <div className="flex flex-col gap-2 border-t border-slate-100 pt-3">
      <Select
        aria-label={t("positionDetail.stepType") as string}
        value={type}
        onChange={(e) => setType(e.target.value as CaseStepType)}
      >
        {CASE_STEP_TYPES.map((stepType) => (
          <option key={stepType} value={stepType}>
            {stepType}
          </option>
        ))}
      </Select>

      <Input
        label={t("positionDetail.stepContent") as string}
        value={content}
        onChange={(e) => setContent(e.target.value)}
      />

      {isTimed && (
        <div className="flex gap-2">
          <Input
            label={t("positionDetail.preparationTimeSeconds") as string}
            type="number"
            min={0}
            value={preparationTimeSeconds}
            onChange={(e) => setPreparationTimeSeconds(e.target.value)}
          />
          <Input
            label={t("positionDetail.recordingTimeSeconds") as string}
            type="number"
            min={0}
            value={recordingTimeSeconds}
            onChange={(e) => setRecordingTimeSeconds(e.target.value)}
          />
        </div>
      )}

      {competencies && competencies.length > 0 && (
        <div className="flex flex-col gap-1">
          <p className="text-sm font-medium text-slate-700">{t("positionDetail.relatedCompetencies")}</p>
          <div className="flex flex-wrap gap-3">
            {competencies.map((competency) => (
              <label key={competency.id} className="flex items-center gap-1.5 text-sm text-slate-700">
                <input
                  type="checkbox"
                  checked={relatedCompetencyIds.includes(competency.id)}
                  onChange={() => toggleCompetency(competency.id)}
                />
                {competency.name}
              </label>
            ))}
          </div>
        </div>
      )}

      <Button onClick={onSubmit} disabled={addFlowStep.isPending || !content.trim()} className="w-fit">
        {t("positionDetail.addStep")}
      </Button>
    </div>
  );
}

function AddFlowForm({ caseBotProjectId, isFirstFlow }: { caseBotProjectId: string; isFirstFlow: boolean }) {
  const { t } = useTranslation();
  const { show } = useToast();
  const addFlow = useAddFlow(caseBotProjectId);
  const [name, setName] = useState("");

  const onSubmit = async () => {
    if (!name.trim()) return;
    try {
      await addFlow.mutateAsync({ name, isDefault: isFirstFlow });
      setName("");
      show(t("positionDetail.flowAdded") as string, "success");
    } catch (error) {
      show(toApiError(error).title, "error");
    }
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle>{t("positionDetail.addFlow")}</CardTitle>
      </CardHeader>
      <div className="flex items-end gap-2">
        <Input label={t("positionDetail.name") as string} value={name} onChange={(e) => setName(e.target.value)} />
        <Button onClick={onSubmit} disabled={addFlow.isPending || !name.trim()}>
          {t("common.add")}
        </Button>
      </div>
    </Card>
  );
}

export function CaseFlowEditorPage() {
  const { t } = useTranslation();
  const { positionId, caseBotProjectId } = useParams<{ positionId: string; caseBotProjectId: string }>();
  const { data: flows, isLoading } = useFlowsForProject(caseBotProjectId);

  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-col gap-1">
        <Link to={`/positions/${positionId}`} className="text-sm text-brand-600 hover:underline">
          ← {t("common.back")}
        </Link>
        <h1 className="text-xl font-semibold text-slate-900">{t("positionDetail.caseQuestions")}</h1>
        <p className="text-sm text-slate-500">{t("positionDetail.caseQuestionsHint")}</p>
      </div>

      {isLoading ? (
        <p className="text-sm text-slate-500">{t("common.loading")}</p>
      ) : (
        (flows ?? []).map((flow) => (
          <Card key={flow.id}>
            <CardHeader>
              <CardTitle>
                {flow.name}
                {flow.isDefault && (
                  <span className="ml-2 rounded bg-brand-50 px-1.5 py-0.5 text-xs font-medium text-brand-600">
                    {t("positionDetail.defaultFlow")}
                  </span>
                )}
              </CardTitle>
            </CardHeader>

            {flow.steps.length > 0 ? (
              <ol className="flex flex-col gap-2">
                {flow.steps.map((step) => (
                  <li key={step.id} className="rounded-md bg-slate-50 p-2 text-sm">
                    <p className="font-medium text-slate-900">
                      {step.order}. {step.type}
                    </p>
                    <p className="text-slate-700">{step.content}</p>
                    {(step.preparationTimeSeconds || step.recordingTimeSeconds) && (
                      <p className="text-xs text-slate-500">
                        {step.preparationTimeSeconds ? `${t("positionDetail.preparationTimeSeconds")}: ${step.preparationTimeSeconds}s` : ""}
                        {step.preparationTimeSeconds && step.recordingTimeSeconds ? " · " : ""}
                        {step.recordingTimeSeconds ? `${t("positionDetail.recordingTimeSeconds")}: ${step.recordingTimeSeconds}s` : ""}
                      </p>
                    )}
                  </li>
                ))}
              </ol>
            ) : (
              <p className="text-sm text-slate-400">{t("positionDetail.noStepsYet")}</p>
            )}

            {caseBotProjectId && <AddFlowStepForm flow={flow} caseBotProjectId={caseBotProjectId} />}
          </Card>
        ))
      )}

      {!isLoading && caseBotProjectId && <AddFlowForm caseBotProjectId={caseBotProjectId} isFirstFlow={(flows ?? []).length === 0} />}
    </div>
  );
}
