import { useParams, Link } from "react-router";
import { useTranslation } from "react-i18next";
import { Card, Badge } from "@peoplise/ui";
import type { CandidatePipelineItem, PipelineStatus } from "@peoplise/api-client";
import { useCandidatePipeline } from "../hooks/useCandidates";

const COLUMNS: PipelineStatus[] = [
  "NewApplication",
  "UnderReview",
  "Testing",
  "Interviewing",
  "Offer",
  "Accepted",
];

const STATUS_LABEL_KEY: Record<PipelineStatus, string> = {
  NewApplication: "pipeline.newApplication",
  UnderReview: "pipeline.underReview",
  Testing: "pipeline.testing",
  Interviewing: "pipeline.interviewing",
  Offer: "pipeline.offer",
  Accepted: "pipeline.accepted",
  Rejected: "pipeline.rejected",
  Eliminated: "pipeline.eliminated",
  TimedOut: "pipeline.timedOut",
};

export function CandidatePipelinePage() {
  const { t } = useTranslation();
  const { positionId } = useParams<{ positionId: string }>();
  const { data, isLoading } = useCandidatePipeline({ positionId: positionId ?? "", pageSize: 100 });

  const byStatus = new Map<PipelineStatus, CandidatePipelineItem[]>();
  for (const item of data?.items ?? []) {
    const bucket = byStatus.get(item.status) ?? [];
    bucket.push(item);
    byStatus.set(item.status, bucket);
  }

  return (
    <div className="flex flex-col gap-4">
      <h1 className="text-xl font-semibold text-slate-900">{t("pipeline.title")}</h1>

      {isLoading ? (
        <p className="text-sm text-slate-500">{t("common.loading")}</p>
      ) : (
        <div className="flex gap-4 overflow-x-auto pb-2">
          {COLUMNS.map((status) => {
            const items = byStatus.get(status) ?? [];
            return (
              <section key={status} aria-label={t(STATUS_LABEL_KEY[status])} className="w-64 shrink-0">
                <h2 className="mb-2 flex items-center justify-between text-sm font-medium text-slate-600">
                  {t(STATUS_LABEL_KEY[status])}
                  <Badge variant="neutral">{items.length}</Badge>
                </h2>
                <ul className="flex flex-col gap-2">
                  {items.map((item) => (
                    <li key={item.candidateProcessId}>
                      <Link to={`/candidates/${item.candidateProcessId}`}>
                        <Card className="hover:border-brand-300">
                          <p className="text-sm font-medium text-slate-900">{item.candidateName}</p>
                          <p className="text-xs text-slate-500">{item.candidateEmail}</p>
                        </Card>
                      </Link>
                    </li>
                  ))}
                  {items.length === 0 && <li className="text-xs text-slate-400">{t("common.noResults")}</li>}
                </ul>
              </section>
            );
          })}
        </div>
      )}
    </div>
  );
}
