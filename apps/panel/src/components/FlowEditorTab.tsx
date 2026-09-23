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
import { Button, Card, Input, Modal, useToast } from "@peoplise/ui";
import { toApiError, type StageType, type WorkflowStage } from "@peoplise/api-client";
import { useAddWorkflowStage, useRemoveWorkflowStage, useReorderWorkflowStages, useWorkflowStages } from "../hooks/useWorkflowStages";

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

function SortableStageCard({ stage, onRemove }: { stage: WorkflowStage; onRemove: (stageId: string) => void }) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({ id: stage.id });
  const style = { transform: CSS.Transform.toString(transform), transition, opacity: isDragging ? 0.5 : 1 };

  return (
    <div ref={setNodeRef} style={style}>
      <Card className="w-48 shrink-0 select-none">
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
                <SortableStageCard key={stage.id} stage={stage} onRemove={onRemoveStage} />
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
          <div className="flex flex-col gap-1.5">
            <label htmlFor="stage-type" className="text-sm font-medium text-slate-700">
              {t("positions.stageType")}
            </label>
            <select
              id="stage-type"
              className="h-10 rounded-md border border-slate-300 bg-white px-3 text-sm text-slate-900 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500"
              {...register("type")}
            >
              {STAGE_TYPES.map((type) => (
                <option key={type} value={type}>
                  {type}
                </option>
              ))}
            </select>
          </div>
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
