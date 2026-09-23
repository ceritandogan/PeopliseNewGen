import { useState } from "react";
import { Link, useParams } from "react-router";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useTranslation } from "react-i18next";
import { Card, CardHeader, CardTitle, Button, Badge, Input, Modal, useToast, cn } from "@peoplise/ui";
import { toApiError, type CaseBotProjectSummary, type BotProjectSummary } from "@peoplise/api-client";
import { usePositionDashboard } from "../hooks/usePositions";
import { useCaseBotProjectsForPosition, useCreateCaseBotProject } from "../hooks/useCaseBotProjects";
import { useBotProjectsForPosition, useCreateBotProject } from "../hooks/useBotProjects";
import { FlowEditorTab } from "../components/FlowEditorTab";

const TABS = ["overview", "flowEditor"] as const;
type Tab = (typeof TABS)[number];

const createCaseBotProjectSchema = z.object({
  name: z.string().min(1),
  retakesAllowed: z.coerce.number().int().min(0),
  retentionPeriodDays: z.coerce.number().int().min(1),
});
type CreateCaseBotProjectForm = z.infer<typeof createCaseBotProjectSchema>;

const createBotProjectSchema = z.object({
  name: z.string().min(1),
  retentionPeriodDays: z.coerce.number().int().min(1),
});
type CreateBotProjectForm = z.infer<typeof createBotProjectSchema>;

export function PositionDetailPage() {
  const { t } = useTranslation();
  const { positionId } = useParams<{ positionId: string }>();
  const [tab, setTab] = useState<Tab>("overview");
  const { data, isLoading } = usePositionDashboard(positionId);

  return (
    <div className="flex flex-col gap-4">
      <h1 className="text-xl font-semibold text-slate-900">{data?.title ?? (isLoading ? t("common.loading") : positionId)}</h1>

      <div role="tablist" aria-label="Position sections" className="flex gap-1 border-b border-slate-200">
        {TABS.map((value) => (
          <button
            key={value}
            role="tab"
            aria-selected={tab === value}
            onClick={() => setTab(value)}
            className={cn(
              "px-4 py-2 text-sm font-medium text-slate-500 hover:text-slate-900",
              tab === value && "border-b-2 border-brand-600 text-brand-700",
            )}
          >
            {value === "overview" ? "Overview" : t("positions.flowEditor")}
          </button>
        ))}
      </div>

      {tab === "overview" && (
        <div className="flex flex-col gap-4">
          <Card>
            <CardHeader>
              <CardTitle>{t("dashboard.candidateDistribution")}</CardTitle>
            </CardHeader>
            {data ? (
              <ul className="flex flex-wrap gap-2">
                {Object.entries(data.applicantsByStatus).map(([status, count]) => (
                  <li key={status}>
                    <Badge variant="neutral">
                      {status}: {count}
                    </Badge>
                  </li>
                ))}
              </ul>
            ) : (
              <p className="text-sm text-slate-500">{t("common.loading")}</p>
            )}
          </Card>

          {positionId && <CaseBotProjectsCard positionId={positionId} />}
          {positionId && <BotProjectsCard positionId={positionId} />}
        </div>
      )}

      {tab === "flowEditor" && positionId && <FlowEditorTab positionId={positionId} />}
    </div>
  );
}

function CaseBotProjectsCard({ positionId }: { positionId: string }) {
  const { t } = useTranslation();
  const { show } = useToast();
  const [isCreateOpen, setCreateOpen] = useState(false);
  const { data, isLoading } = useCaseBotProjectsForPosition(positionId);
  const createProject = useCreateCaseBotProject(positionId);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<CreateCaseBotProjectForm>({
    resolver: zodResolver(createCaseBotProjectSchema),
    defaultValues: { retakesAllowed: 1, retentionPeriodDays: 180 },
  });

  const onSubmit = handleSubmit(async (values) => {
    try {
      await createProject.mutateAsync({ ...values, positionId });
      show("Project created.", "success");
      reset();
      setCreateOpen(false);
    } catch (error) {
      show(toApiError(error).title, "error");
    }
  });

  return (
    <Card>
      <CardHeader>
        <CardTitle>{t("positionDetail.caseBotProjects")}</CardTitle>
        <Button size="sm" variant="outline" onClick={() => setCreateOpen(true)}>
          {t("positionDetail.newCaseBotProject")}
        </Button>
      </CardHeader>

      {isLoading ? (
        <p className="text-sm text-slate-500">{t("common.loading")}</p>
      ) : data && data.length > 0 ? (
        <ul className="flex flex-col gap-2">
          {data.map((project: CaseBotProjectSummary) => (
            <li key={project.id} className="flex flex-col gap-1 text-sm">
              <div className="flex items-center justify-between">
                <span className="font-medium text-slate-900">{project.name}</span>
                <span className="text-slate-500">
                  {t("positionDetail.retakesAllowed")}: {project.retakesAllowed} · {t("positionDetail.retentionPeriodDays")}:{" "}
                  {project.retentionPeriodDays}
                </span>
              </div>
              <Link
                to={`/positions/${positionId}/case-bot-projects/${project.id}/comparison`}
                className="text-brand-600 hover:underline"
              >
                {t("positionDetail.viewComparison")}
              </Link>
            </li>
          ))}
        </ul>
      ) : (
        <p className="text-sm text-slate-500">{t("positionDetail.noProjectsYet")}</p>
      )}

      <Modal open={isCreateOpen} onClose={() => setCreateOpen(false)} title={t("positionDetail.newCaseBotProject")}>
        <form onSubmit={onSubmit} className="flex flex-col gap-3" noValidate>
          <Input label={t("positionDetail.name") as string} error={errors.name?.message} {...register("name")} />
          <Input
            label={t("positionDetail.retakesAllowed") as string}
            type="number"
            error={errors.retakesAllowed?.message}
            {...register("retakesAllowed")}
          />
          <Input
            label={t("positionDetail.retentionPeriodDays") as string}
            type="number"
            error={errors.retentionPeriodDays?.message}
            {...register("retentionPeriodDays")}
          />
          <div className="flex justify-end gap-2 pt-2">
            <Button type="button" variant="outline" onClick={() => setCreateOpen(false)}>
              {t("common.cancel")}
            </Button>
            <Button type="submit" disabled={isSubmitting}>
              {t("common.save")}
            </Button>
          </div>
        </form>
      </Modal>
    </Card>
  );
}

function BotProjectsCard({ positionId }: { positionId: string }) {
  const { t } = useTranslation();
  const { show } = useToast();
  const [isCreateOpen, setCreateOpen] = useState(false);
  const { data, isLoading } = useBotProjectsForPosition(positionId);
  const createProject = useCreateBotProject(positionId);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<CreateBotProjectForm>({
    resolver: zodResolver(createBotProjectSchema),
    defaultValues: { retentionPeriodDays: 180 },
  });

  const onSubmit = handleSubmit(async (values) => {
    try {
      await createProject.mutateAsync({ ...values, positionId });
      show("Project created.", "success");
      reset();
      setCreateOpen(false);
    } catch (error) {
      show(toApiError(error).title, "error");
    }
  });

  return (
    <Card>
      <CardHeader>
        <CardTitle>{t("positionDetail.botProjects")}</CardTitle>
        <Button size="sm" variant="outline" onClick={() => setCreateOpen(true)}>
          {t("positionDetail.newBotProject")}
        </Button>
      </CardHeader>

      {isLoading ? (
        <p className="text-sm text-slate-500">{t("common.loading")}</p>
      ) : data && data.length > 0 ? (
        <ul className="flex flex-col gap-2">
          {data.map((project: BotProjectSummary) => (
            <li key={project.id} className="flex items-center justify-between text-sm">
              <span className="font-medium text-slate-900">{project.name}</span>
              <span className="text-slate-500">
                {t("positionDetail.retentionPeriodDays")}: {project.retentionPeriodDays}
              </span>
            </li>
          ))}
        </ul>
      ) : (
        <p className="text-sm text-slate-500">{t("positionDetail.noProjectsYet")}</p>
      )}

      <Modal open={isCreateOpen} onClose={() => setCreateOpen(false)} title={t("positionDetail.newBotProject")}>
        <form onSubmit={onSubmit} className="flex flex-col gap-3" noValidate>
          <Input label={t("positionDetail.name") as string} error={errors.name?.message} {...register("name")} />
          <Input
            label={t("positionDetail.retentionPeriodDays") as string}
            type="number"
            error={errors.retentionPeriodDays?.message}
            {...register("retentionPeriodDays")}
          />
          <div className="flex justify-end gap-2 pt-2">
            <Button type="button" variant="outline" onClick={() => setCreateOpen(false)}>
              {t("common.cancel")}
            </Button>
            <Button type="submit" disabled={isSubmitting}>
              {t("common.save")}
            </Button>
          </div>
        </form>
      </Modal>
    </Card>
  );
}
