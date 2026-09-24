import { useState } from "react";
import { Link, useParams } from "react-router";
import { useTranslation } from "react-i18next";
import { Button, Card, CardHeader, CardTitle, Input, useToast } from "@peoplise/ui";
import {
  toApiError,
  type BotConditionType,
  type BotFlow,
  type BotStepRouteType,
  type ProjectVariable,
  type StepType,
} from "@peoplise/api-client";
import {
  useAddBotFlow,
  useAddBotStep,
  useAddBotStepRoute,
  useAddVariable,
  useFlowsForBotProject,
  useRemoveBotStepRoute,
  useVariablesForProject,
} from "../hooks/useBotProjects";

const STEP_TYPES: StepType[] = [
  "SendMessage",
  "SendQuickReply",
  "WaitResponse",
  "SendImage",
  "SendVideo",
  "SendEmail",
  "SwitchFlow",
  "FaqEngine",
  "CallWebHook",
];

const CONDITION_TYPES: BotConditionType[] = ["NoCondition", "HasAnyKeywords", "HasAllKeywords", "DoesNotContainKeywords", "HasOnlyKeyword"];
const ROUTE_TYPES: BotStepRouteType[] = ["NextStep", "SwitchFlow", "EndConversation"];

function splitKeywords(raw: string): string[] {
  return raw
    .split(",")
    .map((k) => k.trim())
    .filter(Boolean);
}

function AddStepRouteForm({
  flow,
  flows,
  stepId,
  botProjectId,
}: {
  flow: BotFlow;
  flows: BotFlow[];
  stepId: string;
  botProjectId: string;
}) {
  const { t } = useTranslation();
  const { show } = useToast();
  const addRoute = useAddBotStepRoute(botProjectId);

  const [conditionType, setConditionType] = useState<BotConditionType>("NoCondition");
  const [keywordsText, setKeywordsText] = useState("");
  const [routeType, setRouteType] = useState<BotStepRouteType>("NextStep");
  const [targetFlowId, setTargetFlowId] = useState("");
  const [targetStepId, setTargetStepId] = useState("");

  const needsKeywords = conditionType !== "NoCondition";
  const targetFlow = routeType === "SwitchFlow" ? flows.find((f) => f.id === targetFlowId) : flow;
  const targetStepOptions = routeType === "NextStep" ? flow.steps : (targetFlow?.steps ?? []);

  const onSubmit = async () => {
    if (needsKeywords && !keywordsText.trim()) return;
    if (routeType === "NextStep" && !targetStepId) return;
    if (routeType === "SwitchFlow" && (!targetFlowId || !targetStepId)) return;

    try {
      await addRoute.mutateAsync({
        flowId: flow.id,
        stepId,
        conditionType,
        keywords: needsKeywords ? splitKeywords(keywordsText) : [],
        routeType,
        targetFlowId: routeType === "SwitchFlow" ? targetFlowId : null,
        targetStepId: routeType === "EndConversation" ? null : targetStepId,
      });
      setKeywordsText("");
      setTargetFlowId("");
      setTargetStepId("");
      show(t("positionDetail.routeAdded") as string, "success");
    } catch (error) {
      show(toApiError(error).title, "error");
    }
  };

  return (
    <div className="flex flex-col gap-1.5 rounded-md bg-slate-50 p-2">
      <div className="flex flex-wrap gap-1.5">
        <select
          aria-label={t("positionDetail.conditionType") as string}
          className="h-8 rounded border border-slate-300 bg-white px-1.5 text-xs text-slate-900"
          value={conditionType}
          onChange={(e) => setConditionType(e.target.value as BotConditionType)}
        >
          {CONDITION_TYPES.map((c) => (
            <option key={c} value={c}>
              {t(`positionDetail.condition_${c}`)}
            </option>
          ))}
        </select>

        <select
          aria-label={t("positionDetail.routeType") as string}
          className="h-8 rounded border border-slate-300 bg-white px-1.5 text-xs text-slate-900"
          value={routeType}
          onChange={(e) => {
            setRouteType(e.target.value as BotStepRouteType);
            setTargetFlowId("");
            setTargetStepId("");
          }}
        >
          {ROUTE_TYPES.map((r) => (
            <option key={r} value={r}>
              {t(`positionDetail.routeTypeOption_${r}`)}
            </option>
          ))}
        </select>
      </div>

      {needsKeywords && (
        <input
          aria-label={t("positionDetail.keywords") as string}
          placeholder={t("positionDetail.keywordsPlaceholder") as string}
          className="h-8 rounded border border-slate-300 bg-white px-1.5 text-xs text-slate-900"
          value={keywordsText}
          onChange={(e) => setKeywordsText(e.target.value)}
        />
      )}

      {routeType === "SwitchFlow" && (
        <select
          aria-label={t("positionDetail.targetFlow") as string}
          className="h-8 rounded border border-slate-300 bg-white px-1.5 text-xs text-slate-900"
          value={targetFlowId}
          onChange={(e) => {
            setTargetFlowId(e.target.value);
            setTargetStepId("");
          }}
        >
          <option value="">—</option>
          {flows.map((f) => (
            <option key={f.id} value={f.id}>
              {f.name}
            </option>
          ))}
        </select>
      )}

      {routeType !== "EndConversation" && (
        <select
          aria-label={t("positionDetail.targetStep") as string}
          className="h-8 rounded border border-slate-300 bg-white px-1.5 text-xs text-slate-900"
          value={targetStepId}
          onChange={(e) => setTargetStepId(e.target.value)}
          disabled={routeType === "SwitchFlow" && !targetFlowId}
        >
          <option value="">—</option>
          {targetStepOptions.map((s) => (
            <option key={s.id} value={s.id}>
              {s.order}. {s.content.slice(0, 40)}
            </option>
          ))}
        </select>
      )}

      <Button size="sm" className="h-7 w-fit px-2 text-xs" disabled={addRoute.isPending} onClick={onSubmit}>
        {t("positionDetail.addRoute")}
      </Button>
    </div>
  );
}

function describeRouteTarget(route: BotFlow["steps"][number]["routes"][number], flows: BotFlow[]): string {
  if (route.routeType === "EndConversation") return "";
  const targetFlow = route.targetFlowId ? flows.find((f) => f.id === route.targetFlowId) : undefined;
  const stepsToSearch = targetFlow ? targetFlow.steps : flows.flatMap((f) => f.steps);
  const targetStep = stepsToSearch.find((s) => s.id === route.targetStepId);
  const stepLabel = targetStep ? `${targetStep.order}. ${targetStep.content.slice(0, 30)}` : route.targetStepId;
  return targetFlow ? `${targetFlow.name} / ${stepLabel}` : (stepLabel ?? "");
}

function BotStepRow({ step, flow, flows, botProjectId }: { step: BotFlow["steps"][number]; flow: BotFlow; flows: BotFlow[]; botProjectId: string }) {
  const { t } = useTranslation();
  const { show } = useToast();
  const removeRoute = useRemoveBotStepRoute(botProjectId);

  const onRemoveRoute = async (routeId: string) => {
    try {
      await removeRoute.mutateAsync({ flowId: flow.id, stepId: step.id, routeId });
    } catch (error) {
      show(toApiError(error).title, "error");
    }
  };

  return (
    <li className="rounded-md bg-slate-50 p-2 text-sm">
      <p className="font-medium text-slate-900">
        {step.order}. {step.type}
        {step.isFinalStep && (
          <span className="ml-2 rounded bg-slate-200 px-1.5 py-0.5 text-xs font-medium text-slate-600">
            {step.isScreenOut ? t("positionDetail.screenOutStep") : t("positionDetail.finalStep")}
          </span>
        )}
      </p>
      <p className="text-slate-700">{step.content}</p>
      {step.quickReplyOptions.length > 0 && (
        <p className="text-xs text-slate-500">
          {t("positionDetail.quickReplyOptions")}: {step.quickReplyOptions.join(", ")}
        </p>
      )}
      {step.captureVariableKey && (
        <p className="text-xs text-slate-500">
          {t("positionDetail.captureVariableKey")}: {step.captureVariableKey}
        </p>
      )}

      {!step.isFinalStep && (
        <div className="mt-2 flex flex-col gap-1.5 border-t border-slate-200 pt-2">
          <p className="text-xs font-medium uppercase tracking-wide text-slate-400">{t("positionDetail.routes")}</p>
          {step.routes.length > 0 && (
            <ul className="flex flex-col gap-1">
              {step.routes.map((route) => (
                <li key={route.id} className="flex items-center justify-between gap-1 text-xs text-slate-600">
                  <span className="truncate">
                    {t(`positionDetail.condition_${route.conditionType}`)}
                    {route.keywords.length > 0 ? ` [${route.keywords.join(", ")}]` : ""} →{" "}
                    {t(`positionDetail.routeTypeOption_${route.routeType}`)} {describeRouteTarget(route, flows)}
                  </span>
                  <button
                    type="button"
                    onClick={() => onRemoveRoute(route.id)}
                    className="shrink-0 text-slate-400 hover:text-red-600"
                    aria-label={t("positionDetail.removeRoute") as string}
                  >
                    ×
                  </button>
                </li>
              ))}
            </ul>
          )}
          <AddStepRouteForm flow={flow} flows={flows} stepId={step.id} botProjectId={botProjectId} />
        </div>
      )}
    </li>
  );
}

function AddStepForm({ flow, botProjectId, variables }: { flow: BotFlow; botProjectId: string; variables: ProjectVariable[] }) {
  const { t } = useTranslation();
  const { show } = useToast();
  const addStep = useAddBotStep(botProjectId);

  const nextOrder = flow.steps.length > 0 ? Math.max(...flow.steps.map((s) => s.order)) + 1 : 0;
  const [type, setType] = useState<StepType>("SendMessage");
  const [content, setContent] = useState("");
  const [quickReplyOptionsText, setQuickReplyOptionsText] = useState("");
  const [captureVariableKey, setCaptureVariableKey] = useState("");
  const [isFinalStep, setIsFinalStep] = useState(false);
  const [isScreenOut, setIsScreenOut] = useState(false);

  const onSubmit = async () => {
    if (!content.trim()) return;
    try {
      await addStep.mutateAsync({
        flowId: flow.id,
        type,
        content,
        order: nextOrder,
        quickReplyOptions: type === "SendQuickReply" ? splitKeywords(quickReplyOptionsText) : null,
        captureVariableKey: type === "WaitResponse" && captureVariableKey ? captureVariableKey : null,
        isFinalStep,
        isScreenOut: isFinalStep && isScreenOut,
      });
      setContent("");
      setQuickReplyOptionsText("");
      setCaptureVariableKey("");
      setIsFinalStep(false);
      setIsScreenOut(false);
      show(t("positionDetail.botStepAdded") as string, "success");
    } catch (error) {
      show(toApiError(error).title, "error");
    }
  };

  return (
    <div className="flex flex-col gap-2 border-t border-slate-100 pt-3">
      <select
        aria-label={t("positionDetail.botStepType") as string}
        className="h-9 rounded-md border border-slate-300 bg-white px-2 text-sm text-slate-900 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500"
        value={type}
        onChange={(e) => setType(e.target.value as StepType)}
      >
        {STEP_TYPES.map((stepType) => (
          <option key={stepType} value={stepType}>
            {stepType}
          </option>
        ))}
      </select>

      <Input label={t("positionDetail.botStepContent") as string} value={content} onChange={(e) => setContent(e.target.value)} />

      {type === "SendQuickReply" && (
        <Input
          label={t("positionDetail.quickReplyOptions") as string}
          value={quickReplyOptionsText}
          onChange={(e) => setQuickReplyOptionsText(e.target.value)}
          placeholder={t("positionDetail.commaSeparated") as string}
        />
      )}

      {type === "WaitResponse" && (
        <div className="flex flex-col gap-1.5">
          <span className="text-sm font-medium text-slate-700">{t("positionDetail.captureVariableKey")}</span>
          <select
            aria-label={t("positionDetail.captureVariableKey") as string}
            className="h-10 rounded-md border border-slate-300 bg-white px-3 text-sm text-slate-900 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500"
            value={captureVariableKey}
            onChange={(e) => setCaptureVariableKey(e.target.value)}
          >
            <option value="">—</option>
            {variables.map((v) => (
              <option key={v.id} value={v.key}>
                {v.key}
              </option>
            ))}
          </select>
          {variables.length === 0 && <p className="text-xs text-slate-400">{t("positionDetail.noVariablesYet")}</p>}
        </div>
      )}

      <label className="flex items-center gap-1.5 text-sm text-slate-700">
        <input type="checkbox" checked={isFinalStep} onChange={(e) => setIsFinalStep(e.target.checked)} />
        {t("positionDetail.isFinalStep")}
      </label>

      {isFinalStep && (
        <label className="flex items-center gap-1.5 text-sm text-slate-700">
          <input type="checkbox" checked={isScreenOut} onChange={(e) => setIsScreenOut(e.target.checked)} />
          {t("positionDetail.isScreenOut")}
        </label>
      )}

      <Button onClick={onSubmit} disabled={addStep.isPending || !content.trim()} className="w-fit">
        {t("positionDetail.addBotStep")}
      </Button>
    </div>
  );
}

function AddFlowForm({ botProjectId, isFirstFlow }: { botProjectId: string; isFirstFlow: boolean }) {
  const { t } = useTranslation();
  const { show } = useToast();
  const addFlow = useAddBotFlow(botProjectId);
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

function VariablesSection({ botProjectId, variables }: { botProjectId: string; variables: ProjectVariable[] }) {
  const { t } = useTranslation();
  const { show } = useToast();
  const addVariable = useAddVariable(botProjectId);
  const [key, setKey] = useState("");
  const [description, setDescription] = useState("");

  const onSubmit = async () => {
    if (!key.trim()) return;
    try {
      await addVariable.mutateAsync({ key: key.trim(), description: description.trim() || undefined });
      setKey("");
      setDescription("");
      show(t("positionDetail.variableAdded") as string, "success");
    } catch (error) {
      show(toApiError(error).title, "error");
    }
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle>{t("positionDetail.variables")}</CardTitle>
      </CardHeader>
      <p className="text-sm text-slate-500">{t("positionDetail.variablesHint")}</p>

      {variables.length > 0 ? (
        <ul className="flex flex-col gap-1">
          {variables.map((v) => (
            <li key={v.id} className="text-sm text-slate-700">
              <span className="font-medium text-slate-900">{v.key}</span>
              {v.description ? ` — ${v.description}` : ""}
            </li>
          ))}
        </ul>
      ) : (
        <p className="text-sm text-slate-400">{t("positionDetail.noVariablesYet")}</p>
      )}

      <div className="mt-2 flex items-end gap-2 border-t border-slate-100 pt-3">
        <Input label={t("positionDetail.variableKey") as string} value={key} onChange={(e) => setKey(e.target.value)} />
        <Input
          label={t("positionDetail.description") as string}
          value={description}
          onChange={(e) => setDescription(e.target.value)}
        />
        <Button onClick={onSubmit} disabled={addVariable.isPending || !key.trim()}>
          {t("common.add")}
        </Button>
      </div>
    </Card>
  );
}

export function BotFlowEditorPage() {
  const { t } = useTranslation();
  const { positionId, botProjectId } = useParams<{ positionId: string; botProjectId: string }>();
  const { data: flows, isLoading } = useFlowsForBotProject(botProjectId);
  const { data: variables } = useVariablesForProject(botProjectId);

  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-col gap-1">
        <Link to={`/positions/${positionId}`} className="text-sm text-brand-600 hover:underline">
          ← {t("common.back")}
        </Link>
        <h1 className="text-xl font-semibold text-slate-900">{t("positionDetail.botScript")}</h1>
        <p className="text-sm text-slate-500">{t("positionDetail.botScriptHint")}</p>
      </div>

      {botProjectId && <VariablesSection botProjectId={botProjectId} variables={variables ?? []} />}

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
                  <BotStepRow key={step.id} step={step} flow={flow} flows={flows ?? []} botProjectId={botProjectId!} />
                ))}
              </ol>
            ) : (
              <p className="text-sm text-slate-400">{t("positionDetail.noBotStepsYet")}</p>
            )}

            {botProjectId && <AddStepForm flow={flow} botProjectId={botProjectId} variables={variables ?? []} />}
          </Card>
        ))
      )}

      {!isLoading && botProjectId && <AddFlowForm botProjectId={botProjectId} isFirstFlow={(flows ?? []).length === 0} />}
    </div>
  );
}
