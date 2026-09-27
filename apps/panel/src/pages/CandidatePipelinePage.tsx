import { useEffect, useRef, useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useParams, useSearchParams, Link } from "react-router";
import { useTranslation } from "react-i18next";
import { Card, Badge, Button, Input, Modal, useToast, cn } from "@peoplise/ui";
import { toApiError, type AddCandidateManuallyResponse, type CandidatePipelineItem, type PipelineStatus } from "@peoplise/api-client";
import { useAddCandidateManually, useCandidatePipeline } from "../hooks/useCandidates";
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

const addCandidateSchema = z.object({
  candidateName: z.string().min(1),
  candidateEmail: z.string().email(),
  candidatePhone: z.string().optional(),
  resumeUrl: z.string().optional(),
});
type AddCandidateForm = z.infer<typeof addCandidateSchema>;

const LINK_LABEL_KEY: Record<AddCandidateManuallyResponse["links"][number]["type"], string> = {
  "bot-chat": "addCandidate.botChatLink",
  "video-interview": "addCandidate.videoInterviewLink",
};

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

        <div className="flex items-center gap-3">
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

          {positionId && <AddCandidateButton positionId={positionId} />}
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

function AddCandidateButton({ positionId }: { positionId: string }) {
  const { t } = useTranslation();
  const { show } = useToast();
  const [isOpen, setOpen] = useState(false);
  const [result, setResult] = useState<AddCandidateManuallyResponse | null>(null);
  const addCandidate = useAddCandidateManually(positionId);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<AddCandidateForm>({ resolver: zodResolver(addCandidateSchema) });

  const close = () => {
    setOpen(false);
    setResult(null);
    reset();
  };

  const onSubmit = handleSubmit(async (values) => {
    try {
      const response = await addCandidate.mutateAsync(values);
      setResult(response);
    } catch (error) {
      show(toApiError(error).title, "error");
    }
  });

  return (
    <>
      <Button size="sm" onClick={() => setOpen(true)}>
        {t("addCandidate.button")}
      </Button>

      <Modal open={isOpen} onClose={close} title={t("addCandidate.title")}>
        {result ? (
          <div className="flex flex-col gap-3">
            <p className="text-sm text-slate-600">{t("addCandidate.added")}</p>
            {result.links.length > 0 ? (
              result.links.map((link) => (
                <div key={link.type} className="flex flex-col gap-1">
                  <span className="text-xs font-medium text-slate-500">{t(LINK_LABEL_KEY[link.type])}</span>
                  <CopyableLink url={link.url} />
                </div>
              ))
            ) : (
              <p className="text-sm text-slate-500">{t("addCandidate.noLinksYet")}</p>
            )}
            <div className="flex justify-end pt-2">
              <Button type="button" onClick={close}>
                {t("addCandidate.done")}
              </Button>
            </div>
          </div>
        ) : (
          <form onSubmit={onSubmit} className="flex flex-col gap-3" noValidate>
            <Input label={t("addCandidate.name") as string} error={errors.candidateName?.message} {...register("candidateName")} />
            <Input
              label={t("addCandidate.email") as string}
              type="email"
              error={errors.candidateEmail?.message}
              {...register("candidateEmail")}
            />
            <Input label={t("addCandidate.phone") as string} error={errors.candidatePhone?.message} {...register("candidatePhone")} />
            <Input label={t("addCandidate.resumeUrl") as string} error={errors.resumeUrl?.message} {...register("resumeUrl")} />
            <div className="flex justify-end gap-2 pt-2">
              <Button type="button" variant="outline" onClick={close}>
                {t("common.cancel")}
              </Button>
              <Button type="submit" disabled={isSubmitting}>
                {t("addCandidate.submit")}
              </Button>
            </div>
          </form>
        )}
      </Modal>
    </>
  );
}

function CopyableLink({ url }: { url: string }) {
  const { t } = useTranslation();
  const [copied, setCopied] = useState(false);

  const onCopy = async () => {
    try {
      await navigator.clipboard.writeText(url);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    } catch {
      // Clipboard API can be unavailable/blocked (e.g. insecure context) — the input
      // below is still manually selectable/copyable either way.
    }
  };

  return (
    <div className="flex items-center gap-2">
      <input
        readOnly
        value={url}
        onFocus={(event) => event.target.select()}
        className="h-9 flex-1 rounded-lg border border-slate-300 bg-slate-50 px-3 text-xs text-slate-600"
      />
      <Button type="button" variant="outline" size="sm" onClick={onCopy}>
        {copied ? t("common.copied") : t("common.copy")}
      </Button>
    </div>
  );
}
