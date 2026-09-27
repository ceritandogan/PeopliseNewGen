import { useEffect, useRef, useState } from "react";
import { useParams, useSearchParams, Link } from "react-router";
import { useTranslation } from "react-i18next";
import { Card, Badge, cn } from "@peoplise/ui";
import type { CandidatePipelineItem, PipelineStatus } from "@peoplise/api-client";
import { useCandidatePipeline } from "../hooks/useCandidates";
import { usePositionDashboard } from "../hooks/usePositions";
import { Breadcrumbs } from "../components/Breadcrumbs";

export const BOARD_STATUSES: PipelineStatus[] = [
  "NewApplication",
  "UnderReview",
  "Testing",
  "Interviewing",
  "Offer",
  "Accepted",
];

const HIGHLIGHT_DURATION_MS = 2500;

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

const STATUS_DOT_COLOR: Record<PipelineStatus, string> = {
  NewApplication: "bg-slate-400",
  UnderReview: "bg-amber-400",
  Testing: "bg-sky-400",
  Interviewing: "bg-violet-400",
  Offer: "bg-pink-400",
  Accepted: "bg-emerald-400",
  Rejected: "bg-red-400",
  Eliminated: "bg-red-400",
  TimedOut: "bg-slate-400",
};

/** Plain `.toLowerCase()` neither folds Turkish diacritics (ş/s, ı/i, ğ/g, ü/u, ö/o, ç/c) nor
 * cases "İ"/"I" the Turkish way — without this, searching "sirket" would never match "Şirket". */
function normalizeForSearch(value: string): string {
  return value
    .toLocaleLowerCase("tr")
    .replace(/ı/g, "i")
    .replace(/ğ/g, "g")
    .replace(/ü/g, "u")
    .replace(/ş/g, "s")
    .replace(/ö/g, "o")
    .replace(/ç/g, "c");
}

export function CandidatePipelinePage() {
  const { t } = useTranslation();
  const { positionId } = useParams<{ positionId: string }>();
  const { data, isLoading } = useCandidatePipeline({ positionId: positionId ?? "", pageSize: 100 });
  const { data: position } = usePositionDashboard(positionId);
  const [searchParams] = useSearchParams();
  const [search, setSearch] = useState("");
  const columnRefs = useRef<Partial<Record<PipelineStatus, HTMLElement | null>>>({});

  const requestedStatus = searchParams.get("status") as PipelineStatus | null;
  const [highlightedStatus, setHighlightedStatus] = useState<PipelineStatus | null>(
    requestedStatus && BOARD_STATUSES.includes(requestedStatus) ? requestedStatus : null,
  );

  useEffect(() => {
    if (!highlightedStatus || isLoading) return;
    columnRefs.current[highlightedStatus]?.scrollIntoView({ behavior: "smooth", inline: "center", block: "nearest" });
    const timer = setTimeout(() => setHighlightedStatus(null), HIGHLIGHT_DURATION_MS);
    return () => clearTimeout(timer);
  }, [highlightedStatus, isLoading]);

  const byStatus = new Map<PipelineStatus, CandidatePipelineItem[]>();
  for (const item of data?.items ?? []) {
    const bucket = byStatus.get(item.status) ?? [];
    bucket.push(item);
    byStatus.set(item.status, bucket);
  }

  const query = normalizeForSearch(search.trim());

  return (
    <div className="flex flex-col gap-4">
      <Breadcrumbs
        items={[
          { label: t("nav.positions"), to: "/positions" },
          ...(position ? [{ label: position.title, to: `/positions/${positionId}` }] : []),
          { label: t("nav.pipeline") },
        ]}
      />

      <div className="flex items-center justify-between gap-4">
        <h1 className="text-xl font-semibold text-slate-900">{t("pipeline.title")}</h1>

        <div className="relative w-64">
          <svg
            className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
            strokeWidth={2}
            aria-hidden="true"
          >
            <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-4.35-4.35M17 10a7 7 0 11-14 0 7 7 0 0114 0z" />
          </svg>
          <label htmlFor="pipeline-search" className="sr-only">
            {t("common.search")}
          </label>
          <input
            id="pipeline-search"
            type="search"
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            placeholder={t("common.search")}
            className="h-9 w-full rounded-lg border border-slate-300 bg-white pl-9 pr-3 text-sm text-slate-900 placeholder:text-slate-400 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500"
          />
        </div>
      </div>

      {isLoading ? (
        <p className="text-sm text-slate-500">{t("common.loading")}</p>
      ) : (
        <div className="flex gap-4 overflow-x-auto pb-2">
          {BOARD_STATUSES.map((status) => {
            const items = (byStatus.get(status) ?? []).filter((item) =>
              normalizeForSearch(item.candidateName).includes(query),
            );
            return (
              <section
                key={status}
                ref={(node) => {
                  columnRefs.current[status] = node;
                }}
                aria-label={t(STATUS_LABEL_KEY[status])}
                className={cn(
                  "w-64 shrink-0 rounded-md transition-shadow duration-300",
                  status === highlightedStatus && "ring-2 ring-brand-500",
                )}
              >
                <h2 className="mb-2 flex items-center justify-between text-sm font-medium text-slate-600">
                  <span className="flex items-center gap-2">
                    <span className={cn("h-2 w-2 rounded-full", STATUS_DOT_COLOR[status])} aria-hidden="true" />
                    {t(STATUS_LABEL_KEY[status])}
                  </span>
                  <Badge variant="neutral">{items.length}</Badge>
                </h2>
                <ul className="flex flex-col gap-2">
                  {items.map((item) => (
                    <li key={item.candidateProcessId}>
                      <Link to={`/candidates/${item.candidateProcessId}`}>
                        <Card className="p-3 transition-shadow hover:border-brand-300 hover:shadow-md">
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
