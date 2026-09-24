import { useState } from "react";
import { Link, useParams } from "react-router";
import { useTranslation } from "react-i18next";
import { Button, Card, CardHeader, CardTitle, Input, Select, useToast } from "@peoplise/ui";
import { toApiError, type Competency } from "@peoplise/api-client";
import {
  useAddCompetency,
  useAddCompetencyIndicator,
  useAddCompetencyLevel,
  useCompetenciesForProject,
} from "../hooks/useCases";

const LEVELS = [1, 2, 3, 4, 5];

function AddLevelForm({ competency, caseBotProjectId }: { competency: Competency; caseBotProjectId: string }) {
  const { t } = useTranslation();
  const { show } = useToast();
  const addLevel = useAddCompetencyLevel(caseBotProjectId);
  const usedLevels = new Set(competency.levels.map((l) => l.level));
  const firstAvailable = LEVELS.find((l) => !usedLevels.has(l)) ?? LEVELS[0];

  const [level, setLevel] = useState(firstAvailable);
  const [description, setDescription] = useState("");

  const onSubmit = async () => {
    if (!description.trim()) return;
    try {
      await addLevel.mutateAsync({ competencyId: competency.id, level, description });
      setDescription("");
      const nextUsed = new Set([...usedLevels, level]);
      setLevel(LEVELS.find((l) => !nextUsed.has(l)) ?? LEVELS[0]);
      show(t("positionDetail.levelAdded") as string, "success");
    } catch (error) {
      show(toApiError(error).title, "error");
    }
  };

  return (
    <div className="flex items-end gap-2">
      <Select label={t("positionDetail.level") as string} value={level} onChange={(e) => setLevel(Number(e.target.value))}>
        {LEVELS.map((l) => (
          <option key={l} value={l}>
            {l}
          </option>
        ))}
      </Select>
      <Input
        label={t("positionDetail.levelDescription") as string}
        value={description}
        onChange={(e) => setDescription(e.target.value)}
        className="flex-1"
      />
      <Button onClick={onSubmit} disabled={addLevel.isPending || !description.trim()}>
        {t("common.add")}
      </Button>
    </div>
  );
}

function AddIndicatorForm({ competency, caseBotProjectId }: { competency: Competency; caseBotProjectId: string }) {
  const { t } = useTranslation();
  const { show } = useToast();
  const addIndicator = useAddCompetencyIndicator(caseBotProjectId);
  const [description, setDescription] = useState("");

  const onSubmit = async () => {
    if (!description.trim()) return;
    try {
      await addIndicator.mutateAsync({ competencyId: competency.id, description });
      setDescription("");
      show(t("positionDetail.indicatorAdded") as string, "success");
    } catch (error) {
      show(toApiError(error).title, "error");
    }
  };

  return (
    <div className="flex items-end gap-2">
      <Input
        label={t("positionDetail.indicatorDescription") as string}
        value={description}
        onChange={(e) => setDescription(e.target.value)}
        className="flex-1"
      />
      <Button onClick={onSubmit} disabled={addIndicator.isPending || !description.trim()}>
        {t("common.add")}
      </Button>
    </div>
  );
}

function CompetencyCard({ competency, caseBotProjectId }: { competency: Competency; caseBotProjectId: string }) {
  const { t } = useTranslation();
  const sortedLevels = [...competency.levels].sort((a, b) => a.level - b.level);

  return (
    <Card>
      <CardHeader>
        <CardTitle>{competency.name}</CardTitle>
      </CardHeader>
      {competency.description && <p className="text-sm text-slate-500">{competency.description}</p>}

      <div className="mt-2 flex flex-col gap-2 border-t border-slate-100 pt-3">
        <p className="text-xs font-medium uppercase tracking-wide text-slate-400">{t("positionDetail.levels")}</p>
        {sortedLevels.length > 0 ? (
          <ul className="flex flex-col gap-1">
            {sortedLevels.map((l) => (
              <li key={l.id} className="text-sm text-slate-700">
                <span className="font-medium text-slate-900">{l.level}.</span> {l.description}
              </li>
            ))}
          </ul>
        ) : (
          <p className="text-sm text-slate-400">{t("positionDetail.noLevelsYet")}</p>
        )}
        <AddLevelForm competency={competency} caseBotProjectId={caseBotProjectId} />
      </div>

      <div className="mt-2 flex flex-col gap-2 border-t border-slate-100 pt-3">
        <p className="text-xs font-medium uppercase tracking-wide text-slate-400">{t("positionDetail.indicators")}</p>
        {competency.indicators.length > 0 ? (
          <ul className="flex list-disc flex-col gap-1 pl-4">
            {competency.indicators.map((i) => (
              <li key={i.id} className="text-sm text-slate-700">
                {i.description}
              </li>
            ))}
          </ul>
        ) : (
          <p className="text-sm text-slate-400">{t("positionDetail.noIndicatorsYet")}</p>
        )}
        <AddIndicatorForm competency={competency} caseBotProjectId={caseBotProjectId} />
      </div>
    </Card>
  );
}

function AddCompetencyForm({ caseBotProjectId }: { caseBotProjectId: string }) {
  const { t } = useTranslation();
  const { show } = useToast();
  const addCompetency = useAddCompetency(caseBotProjectId);
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");

  const onSubmit = async () => {
    if (!name.trim()) return;
    try {
      await addCompetency.mutateAsync({ name, description: description.trim() || undefined });
      setName("");
      setDescription("");
      show(t("positionDetail.competencyAdded") as string, "success");
    } catch (error) {
      show(toApiError(error).title, "error");
    }
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle>{t("positionDetail.addCompetency")}</CardTitle>
      </CardHeader>
      <div className="flex flex-col gap-2">
        <Input label={t("positionDetail.name") as string} value={name} onChange={(e) => setName(e.target.value)} />
        <Input
          label={t("positionDetail.description") as string}
          value={description}
          onChange={(e) => setDescription(e.target.value)}
        />
        <Button onClick={onSubmit} disabled={addCompetency.isPending || !name.trim()} className="w-fit">
          {t("common.add")}
        </Button>
      </div>
    </Card>
  );
}

export function CompetencyEditorPage() {
  const { t } = useTranslation();
  const { positionId, caseBotProjectId } = useParams<{ positionId: string; caseBotProjectId: string }>();
  const { data: competencies, isLoading } = useCompetenciesForProject(caseBotProjectId);

  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-col gap-1">
        <Link to={`/positions/${positionId}`} className="text-sm text-brand-600 hover:underline">
          ← {t("common.back")}
        </Link>
        <h1 className="text-xl font-semibold text-slate-900">{t("positionDetail.competencies")}</h1>
        <p className="text-sm text-slate-500">{t("positionDetail.competenciesHint")}</p>
      </div>

      {isLoading ? (
        <p className="text-sm text-slate-500">{t("common.loading")}</p>
      ) : (
        (competencies ?? []).map((competency) => (
          <CompetencyCard key={competency.id} competency={competency} caseBotProjectId={caseBotProjectId!} />
        ))
      )}

      {!isLoading && caseBotProjectId && <AddCompetencyForm caseBotProjectId={caseBotProjectId} />}
    </div>
  );
}
