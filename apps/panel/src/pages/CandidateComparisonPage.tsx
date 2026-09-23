import { Link, useParams } from "react-router";
import { useTranslation } from "react-i18next";
import { Card, DataTable, type DataTableColumn } from "@peoplise/ui";
import type { CandidateComparisonItem } from "@peoplise/api-client";
import { useCandidateComparison, useCompetenciesForProject } from "../hooks/useCases";
import { useCandidateNames } from "../hooks/useCandidates";
import { CompetencyComparisonChart } from "../components/CompetencyComparisonChart";

export function CandidateComparisonPage() {
  const { t } = useTranslation();
  const { positionId, caseBotProjectId } = useParams<{ positionId: string; caseBotProjectId: string }>();
  const { data: items, isLoading: itemsLoading } = useCandidateComparison(caseBotProjectId);
  const { data: competencies, isLoading: competenciesLoading } = useCompetenciesForProject(caseBotProjectId);

  const candidateIds = (items ?? []).map((item) => item.candidateId);
  const { data: names, isLoading: namesLoading } = useCandidateNames(positionId, candidateIds);

  const isLoading = itemsLoading || competenciesLoading || (candidateIds.length > 0 && namesLoading);

  const columns: DataTableColumn<CandidateComparisonItem>[] = [
    {
      key: "rank",
      header: t("comparison.rank"),
      render: (row) => (items ?? []).findIndex((item) => item.caseId === row.caseId) + 1,
    },
    {
      key: "candidate",
      header: t("comparison.candidate"),
      render: (row) => {
        const lookup = names?.[row.candidateId];
        if (!lookup) return row.candidateId;
        return (
          <Link to={`/candidates/${lookup.candidateProcessId}`} className="font-medium text-brand-600 hover:underline">
            {lookup.name}
          </Link>
        );
      },
    },
    {
      key: "overallScore",
      header: t("comparison.overallScore"),
      render: (row) => row.overallScore.toFixed(2),
    },
    ...(competencies ?? []).map((competency) => ({
      key: competency.id,
      header: competency.name,
      render: (row: CandidateComparisonItem) => {
        const score = row.competencyScores[competency.id];
        return score === undefined ? <span className="text-slate-400">{t("comparison.notScored")}</span> : score.toFixed(2);
      },
    })),
  ];

  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-col gap-1">
        <Link to={`/positions/${positionId}`} className="text-sm text-brand-600 hover:underline">
          ← {t("common.back")}
        </Link>
        <h1 className="text-xl font-semibold text-slate-900">{t("comparison.title")}</h1>
      </div>

      <CompetencyComparisonChart items={items ?? []} competencies={competencies ?? []} names={names} />

      <Card>
        <DataTable
          columns={columns}
          rows={items ?? []}
          getRowKey={(row) => row.caseId}
          isLoading={isLoading}
          emptyMessage={t("comparison.noCandidatesYet") as string}
        />
      </Card>
    </div>
  );
}
