import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useTranslation } from "react-i18next";
import {
  DndContext,
  closestCenter,
  PointerSensor,
  KeyboardSensor,
  useSensor,
  useSensors,
  type DragEndEvent,
} from "@dnd-kit/core";
import { SortableContext, arrayMove, horizontalListSortingStrategy, sortableKeyboardCoordinates, useSortable } from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { Button, Card, Input, Modal, Select, useToast } from "@peoplise/ui";
import { toApiError, type StageRuleType, type StageType, type WorkflowStage } from "@peoplise/api-client";
import {
  useAddStageRule,
  useAddWorkflowStage,
  useRemoveStageRule,
  useRemoveWorkflowStage,
  useReorderWorkflowStages,
  useWorkflowStages,
} from "../hooks/useWorkflowStages";

const STAGE_TYPES: StageType[] = [
  "InformationForm",
  "ScreeningTest",
  "VideoInterview",
  "DocumentCollection",
  "LiveInterview",
  "ReviewerApproval",
  "OfferStage",
];

const addStageSchema = z.object({
  name: z.string().min(1),
  type: z.string().min(1),
});
type AddStageForm = z.infer<typeof addStageSchema>;

const STAGE_RULE_TYPES: StageRuleType[] = ["AdvanceIfScoreAtLeast", "EliminateIfScoreBelow", "ActivateAfterDelay"];

/** ActivateAfterDelay needs delayDays; the other two need a 0-100 threshold — see AddStageRuleCommandValidator's own remarks. */
function ruleUsesDelay(type: StageRuleType) {
  return type === "ActivateAfterDelay";
}

function StageRuleRow({
  rule,
  onRemove,
}: {
  rule: WorkflowStage["rules"][number];
  onRemove: (ruleId: string) => void;
}) {
  const { t } = useTranslation();
  const detail = ruleUsesDelay(rule.type)
    ? t("positions.ruleDelayDetail", { days: rule.delayDays })
    : t("positions.ruleThresholdDetail", { threshold: rule.threshold });

  return (
    <li className="flex items-center justify-between gap-1 text-xs text-slate-600">
      <span className="truncate">
        {t(`positions.ruleType_${rule.type}`)} {detail}
      </span>
      <button type="button" onClick={() => onRemove(rule.id)} className="shrink-0 text-slate-400 hover:text-red-600" aria-label={t("positions.removeRule") as string}>
        ×
      </button>
    </li>
  );
}

function AddStageRuleForm({ onAdd }: { onAdd: (type: StageRuleType, threshold: number | null, delayDays: number | null) => Promise<void> }) {
  const { t } = useTranslation();
  const [type, setType] = useState<StageRuleType>("AdvanceIfScoreAtLeast");
  const [value, setValue] = useState("");
  const [isSubmitting, setSubmitting] = useState(false);
  const usesDelay = ruleUsesDelay(type);

  const onSubmit = async () => {
    const parsed = Number(value);
    if (!value || Number.isNaN(parsed)) return;

    setSubmitting(true);
    try {
      await onAdd(type, usesDelay ? null : parsed, usesDelay ? parsed : null);
      setValue("");
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="flex flex-col gap-1 pt-1">
      <Select
        size="sm"
        aria-label={t("positions.ruleType") as string}
        value={type}
        onChange={(e) => {
          setType(e.target.value as StageRuleType);
          setValue("");
        }}
      >
        {STAGE_RULE_TYPES.map((ruleType) => (
          <option key={ruleType} value={ruleType}>
            {t(`positions.ruleType_${ruleType}`)}
          </option>
        ))}
      </Select>
      <div className="flex gap-1">
        <input
          type="number"
          min={usesDelay ? 1 : 0}
          max={usesDelay ? undefined : 100}
          value={value}
          onChange={(e) => setValue(e.target.value)}
          placeholder={usesDelay ? (t("positions.delayDaysPlaceholder") as string) : (t("positions.thresholdPlaceholder") as string)}
          className="h-7 w-full rounded border border-slate-300 bg-white px-1.5 text-xs text-slate-900 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500"
        />
        <Button size="sm" className="h-7 shrink-0 px-2 text-xs" disabled={isSubmitting} onClick={onSubmit}>
          {t("common.add")}
        </Button>
      </div>
    </div>
  );
}

function SortableStageCard({
  stage,
  onRemove,
  onAddRule,
  onRemoveRule,
}: {
  stage: WorkflowStage;
  onRemove: (stageId: string) => void;
  onAddRule: (stageId: string, type: StageRuleType, threshold: number | null, delayDays: number | null) => Promise<void>;
  onRemoveRule: (stageId: string, ruleId: string) => void;
}) {
  const { t } = useTranslation();
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({ id: stage.id });
  const style = { transform: CSS.Transform.toString(transform), transition, opacity: isDragging ? 0.5 : 1 };

  return (
    <div ref={setNodeRef} style={style}>
      <Card className="w-56 shrink-0 select-none">
        <div className="mb-1 flex items-center justify-between">
          <button
            type="button"
            {...attributes}
            {...listeners}
            className="cursor-grab touch-none text-slate-400 hover:text-slate-600"
            aria-label="Drag to reorder"
          >
            ⠿
          </button>
          <button type="button" onClick={() => onRemove(stage.id)} className="text-slate-400 hover:text-red-600" aria-label="Remove stage">
            ×
          </button>
        </div>
        <p className="text-sm font-medium text-slate-900">{stage.name}</p>
        <p className="text-xs text-slate-500">{stage.type}</p>

        <div className="mt-2 border-t border-slate-100 pt-2">
          <p className="text-[11px] font-medium uppercase tracking-wide text-slate-400">{t("positions.rules")}</p>
          {stage.rules.length > 0 ? (
            <ul className="mt-1 flex flex-col gap-0.5">
              {stage.rules.map((rule) => (
                <StageRuleRow key={rule.id} rule={rule} onRemove={(ruleId) => onRemoveRule(stage.id, ruleId)} />
              ))}
            </ul>
          ) : null}
          <AddStageRuleForm onAdd={(type, threshold, delayDays) => onAddRule(stage.id, type, threshold, delayDays)} />
        </div>
      </Card>
    </div>
  );
}

export function FlowEditorTab({ positionId }: { positionId: string }) {
  const { t } = useTranslation();
  const { show } = useToast();
  const { data, isLoading } = useWorkflowStages(positionId);
  const workflowDefinitionId = data?.workflowDefinitionId;
  const addStage = useAddWorkflowStage(positionId, workflowDefinitionId);
  const reorderStages = useReorderWorkflowStages(positionId, workflowDefinitionId);
  const removeStage = useRemoveWorkflowStage(positionId, workflowDefinitionId);
  const addStageRule = useAddStageRule(positionId, workflowDefinitionId);
  const removeStageRule = useRemoveStageRule(positionId, workflowDefinitionId);
  const [isAddOpen, setAddOpen] = useState(false);

  const sensors = useSensors(useSensor(PointerSensor), useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates }));

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<AddStageForm>({ resolver: zodResolver(addStageSchema), defaultValues: { type: STAGE_TYPES[0] } });

  const stages = data?.stages ?? [];

  const onAddStage = handleSubmit(async (values) => {
    try {
      const nextOrder = stages.length > 0 ? Math.max(...stages.map((s) => s.order)) + 1 : 0;
      await addStage.mutateAsync({ name: values.name, type: values.type as StageType, order: nextOrder });
      reset({ name: "", type: STAGE_TYPES[0] });
      setAddOpen(false);
    } catch (error) {
      show(toApiError(error).title, "error");
    }
  });

  const onRemoveStage = async (stageId: string) => {
    try {
      await removeStage.mutateAsync(stageId);
    } catch (error) {
      show(toApiError(error).title, "error");
    }
  };

  const onAddRule = async (stageId: string, type: StageRuleType, threshold: number | null, delayDays: number | null) => {
    try {
      await addStageRule.mutateAsync({ stageId, type, threshold, delayDays });
    } catch (error) {
      show(toApiError(error).title, "error");
    }
  };

  const onRemoveRule = async (stageId: string, ruleId: string) => {
    try {
      await removeStageRule.mutateAsync({ stageId, ruleId });
    } catch (error) {
      show(toApiError(error).title, "error");
    }
  };

  const onDragEnd = async (event: DragEndEvent) => {
    const { active, over } = event;
    if (!over || active.id === over.id) return;

    const oldIndex = stages.findIndex((s) => s.id === active.id);
    const newIndex = stages.findIndex((s) => s.id === over.id);
    const reordered = arrayMove(stages, oldIndex, newIndex);

    try {
      await reorderStages.mutateAsync(reordered.map((s) => s.id));
    } catch (error) {
      show(toApiError(error).title, "error");
    }
  };

  if (isLoading) return <p className="text-sm text-slate-500">{t("common.loading")}</p>;

  return (
    <div className="flex flex-col gap-3">
      <p className="text-sm text-slate-500">{t("positions.flowEditorHint")}</p>

      {stages.length === 0 ? (
        <p className="text-sm text-slate-400">{t("positions.noStagesYet")}</p>
      ) : (
        <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={onDragEnd}>
          <SortableContext items={stages.map((s) => s.id)} strategy={horizontalListSortingStrategy}>
            <div className="flex gap-3 overflow-x-auto pb-2" aria-label="Workflow stages">
              {stages.map((stage) => (
                <SortableStageCard
                  key={stage.id}
                  stage={stage}
                  onRemove={onRemoveStage}
                  onAddRule={onAddRule}
                  onRemoveRule={onRemoveRule}
                />
              ))}
            </div>
          </SortableContext>
        </DndContext>
      )}

      <Button variant="outline" className="w-fit" onClick={() => setAddOpen(true)}>
        {t("positions.addStage")}
      </Button>

      <Modal open={isAddOpen} onClose={() => setAddOpen(false)} title={t("positions.addStage") as string}>
        <form onSubmit={onAddStage} className="flex flex-col gap-3" noValidate>
          <Input label={t("positionDetail.name") as string} error={errors.name?.message} {...register("name")} />
          <Select label={t("positions.stageType") as string} {...register("type")}>
            {STAGE_TYPES.map((type) => (
              <option key={type} value={type}>
                {type}
              </option>
            ))}
          </Select>
          <div className="flex justify-end gap-2 pt-2">
            <Button type="button" variant="outline" onClick={() => setAddOpen(false)}>
              {t("common.cancel")}
            </Button>
            <Button type="submit" disabled={isSubmitting}>
              {t("common.save")}
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  );
}
