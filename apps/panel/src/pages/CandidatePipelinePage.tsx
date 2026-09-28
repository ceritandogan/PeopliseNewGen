import { useEffect, useRef, useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useParams, useSearchParams, Link } from "react-router";
import { useTranslation } from "react-i18next";
import { Card, Badge, Button, Input, Modal, useToast, useAuth, cn } from "@peoplise/ui";
import { toApiError, type AddCandidateManuallyResponse, type CandidatePipelineItem, type PipelineStatus } from "@peoplise/api-client";
import { useAddCandidateManually, useCandidatePipeline } from "../hooks/useCandidates";
import { usePositionDashboard } from "../hooks/usePositions";
import { Breadcrumbs } from "../components/Breadcrumbs";
import { avatarColorFor, hashSeed } from "../lib/avatarColor";

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

const PIPELINE_TABS = ["applicants", "aboutJobs", "discussion", "schedule"] as const;
type PipelineTab = (typeof PIPELINE_TABS)[number];

const TAB_LABEL_KEY: Record<PipelineTab, string> = {
  applicants: "pipeline.tabApplicants",
  aboutJobs: "pipeline.tabAboutJobs",
  discussion: "pipeline.tabDiscussion",
  schedule: "pipeline.tabSchedule",
};

/**
 * Visual-only decoration for the Dribbble-referenced board redesign (grilled: per-candidate
 * invented values, deliberately). Derived entirely client-side from the candidate's own id —
 * never sent to the backend, never persisted, never shown anywhere outside this one board.
 * Company names are invented/generic on purpose, not real employers, to avoid attaching a
 * specific false employment claim to a real person's name.
 */
const FAKE_LEVEL_KEYS = ["pipeline.levelJunior", "pipeline.levelMid", "pipeline.levelSenior", "pipeline.levelPrincipal"];
const FAKE_COMPANIES = [
  "Nova Systems",
  "BluePeak Labs",
  "Vertex Digital",
  "Northwind Tech",
  "Solara Inc.",
  "Cascade Works",
  "Ironclad Software",
  "Lumen Analytics",
];

function fakeCandidateMeta(candidateProcessId: string) {
  const hash = hashSeed(candidateProcessId);
  return {
    levelKey: FAKE_LEVEL_KEYS[hash % FAKE_LEVEL_KEYS.length],
    experienceYears: (hash % 7) + 2,
    company: FAKE_COMPANIES[Math.floor(hash / FAKE_LEVEL_KEYS.length) % FAKE_COMPANIES.length],
    hoursAgo: (hash % 20) + 1,
    comments: hash % 13,
    attachments: hash % 4,
  };
}

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
  const { show } = useToast();
  const { session } = useAuth();
  const { positionId } = useParams<{ positionId: string }>();
  const { data, isLoading } = useCandidatePipeline({ positionId: positionId ?? "", pageSize: 100 });
  const { data: position } = usePositionDashboard(positionId);
  const [searchParams] = useSearchParams();
  const [search, setSearch] = useState("");
  const [tab, setTab] = useState<PipelineTab>("applicants");
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

  /** For chrome borrowed purely for visual fidelity to the reference design — nothing behind it yet. */
  const notImplemented = () => show(t("pipeline.comingSoon") as string, "info");

  const selectTab = (value: PipelineTab) => {
    setTab(value);
    if (value !== "applicants") notImplemented();
  };

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

      <Card className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex min-w-0 items-center gap-3">
          <div
            className={cn(
              "flex h-12 w-12 shrink-0 items-center justify-center rounded-xl text-lg font-semibold",
              position ? avatarColorFor(position.title) : "bg-slate-100 text-slate-400",
            )}
            aria-hidden="true"
          >
            {(position?.title ?? "?").slice(0, 1).toUpperCase()}
          </div>
          <div className="min-w-0">
            <div className="flex flex-wrap items-center gap-2">
              <h1 className="truncate text-xl font-semibold text-slate-900">{position?.title ?? t("common.loading")}</h1>
              <Badge variant="success" className="shrink-0 gap-1.5">
                <span className="h-1.5 w-1.5 rounded-full bg-emerald-500" aria-hidden="true" />
                {t("pipeline.activelyHiring")}
              </Badge>
            </div>
            <p className="text-sm text-slate-500">
              {t("dashboard.totalApplicants")}: {position?.totalApplicants ?? "–"}
            </p>
          </div>
        </div>

        <div className="flex shrink-0 items-center gap-3">
          <div className="flex -space-x-2">
            {session?.user.email && (
              <div
                title={session.user.email}
                className={cn(
                  "flex h-8 w-8 items-center justify-center rounded-full border-2 border-white text-xs font-semibold",
                  avatarColorFor(session.user.email),
                )}
              >
                {session.user.email.slice(0, 1).toUpperCase()}
              </div>
            )}
            <button
              type="button"
              onClick={notImplemented}
              aria-label={t("pipeline.invite") as string}
              className="flex h-8 w-8 items-center justify-center rounded-full border-2 border-dashed border-slate-300 bg-white text-slate-400 hover:border-brand-400 hover:text-brand-600"
            >
              +
            </button>
          </div>
          <Button variant="outline" size="sm" onClick={notImplemented}>
            {t("pipeline.invite")}
          </Button>
        </div>
      </Card>

      <div role="tablist" aria-label={t("pipeline.title") as string} className="flex gap-1 border-b border-slate-200">
        {PIPELINE_TABS.map((value) => (
          <button
            key={value}
            role="tab"
            aria-selected={tab === value}
            onClick={() => selectTab(value)}
            className={cn(
              "px-4 py-2 text-sm font-medium text-slate-500 hover:text-slate-900",
              tab === value && "border-b-2 border-brand-600 text-brand-700",
            )}
          >
            {t(TAB_LABEL_KEY[value])}
          </button>
        ))}
      </div>

      {tab !== "applicants" ? (
        <Card className="flex flex-col items-center gap-1 py-16 text-center">
          <p className="text-sm font-medium text-slate-500">{t("pipeline.comingSoon")}</p>
        </Card>
      ) : (
        <>
          <div className="flex flex-wrap items-center justify-between gap-3">
            <div className="flex flex-wrap items-center gap-2">
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
              <Button variant="outline" size="sm" onClick={notImplemented}>
                <FilterIcon />
                {t("pipeline.filter")}
              </Button>
            </div>

            <div className="flex items-center gap-2">
              <div className="flex items-center gap-1 rounded-lg border border-slate-200 p-1">
                <button
                  type="button"
                  aria-label="Grid view"
                  onClick={notImplemented}
                  className="rounded-md p-1.5 text-slate-400 hover:bg-slate-100"
                >
                  <GridIcon />
                </button>
                <button
                  type="button"
                  aria-label="List view"
                  onClick={notImplemented}
                  className="rounded-md p-1.5 text-slate-400 hover:bg-slate-100"
                >
                  <ListIcon />
                </button>
                <button type="button" aria-label="Board view" aria-pressed="true" className="rounded-md bg-brand-50 p-1.5 text-brand-600">
                  <BoardIcon />
                </button>
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
                      "w-72 shrink-0 rounded-md transition-shadow duration-300",
                      status === highlightedStatus && "ring-2 ring-brand-500",
                    )}
                  >
                    <h2 className="mb-2 flex items-center justify-between text-sm font-medium text-slate-600">
                      <span className="flex items-center gap-2">
                        <span className={cn("h-2 w-2 rounded-full", STATUS_DOT_COLOR[status])} aria-hidden="true" />
                        {t(STATUS_LABEL_KEY[status])}
                        <Badge variant="neutral">{items.length}</Badge>
                      </span>
                      <button
                        type="button"
                        onClick={notImplemented}
                        title={t("pipeline.aiSummary") as string}
                        aria-label={t("pipeline.aiSummary") as string}
                        className="rounded-md p-1 text-slate-400 hover:bg-slate-100 hover:text-brand-600"
                      >
                        ✨
                      </button>
                    </h2>
                    <ul className="flex flex-col gap-2">
                      {items.map((item) => {
                        const meta = fakeCandidateMeta(item.candidateProcessId);
                        return (
                          <li key={item.candidateProcessId}>
                            <Link to={`/candidates/${item.candidateProcessId}`}>
                              <Card className="flex flex-col gap-2.5 p-3 transition-shadow hover:border-brand-300 hover:shadow-md">
                                <div className="flex items-center gap-2.5">
                                  <div
                                    className={cn(
                                      "flex h-8 w-8 shrink-0 items-center justify-center rounded-full text-xs font-semibold",
                                      avatarColorFor(item.candidateName),
                                    )}
                                    aria-hidden="true"
                                  >
                                    {item.candidateName.slice(0, 1).toUpperCase()}
                                  </div>
                                  <div className="min-w-0">
                                    <p className="truncate text-sm font-medium text-slate-900">{item.candidateName}</p>
                                    <p className="text-xs text-slate-500">{t(meta.levelKey)}</p>
                                  </div>
                                </div>

                                <div className="flex flex-col gap-1 border-t border-slate-100 pt-2 text-xs text-slate-500">
                                  <span className="flex items-center gap-1.5">
                                    <ClockIcon />
                                    {t("pipeline.yearsExperience", { count: meta.experienceYears })}
                                  </span>
                                  <span className="flex items-center gap-1.5">
                                    <BuildingIcon />
                                    {t("pipeline.exEmployer", { company: meta.company })}
                                  </span>
                                </div>

                                <div className="flex items-center justify-between text-xs text-slate-400">
                                  <span>{t("pipeline.hoursAgo", { count: meta.hoursAgo })}</span>
                                  <span className="flex items-center gap-2.5">
                                    {meta.attachments > 0 && (
                                      <span className="flex items-center gap-1">
                                        <PaperclipIcon />
                                        {meta.attachments}
                                      </span>
                                    )}
                                    <span className="flex items-center gap-1">
                                      <CommentIcon />
                                      {meta.comments}
                                    </span>
                                  </span>
                                </div>
                              </Card>
                            </Link>
                          </li>
                        );
                      })}
                      {items.length === 0 && <li className="text-xs text-slate-400">{t("common.noResults")}</li>}
                    </ul>
                  </section>
                );
              })}
            </div>
          )}
        </>
      )}
    </div>
  );
}

function FilterIcon() {
  return (
    <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2} aria-hidden="true">
      <path strokeLinecap="round" strokeLinejoin="round" d="M4 5h16l-6 7v6l-4 2v-8L4 5z" />
    </svg>
  );
}

function GridIcon() {
  return (
    <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2} aria-hidden="true">
      <rect x="3" y="3" width="7" height="7" rx="1" />
      <rect x="14" y="3" width="7" height="7" rx="1" />
      <rect x="3" y="14" width="7" height="7" rx="1" />
      <rect x="14" y="14" width="7" height="7" rx="1" />
    </svg>
  );
}

function ListIcon() {
  return (
    <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2} aria-hidden="true">
      <path strokeLinecap="round" d="M4 6h16M4 12h16M4 18h16" />
    </svg>
  );
}

function BoardIcon() {
  return (
    <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2} aria-hidden="true">
      <rect x="3" y="4" width="5" height="16" rx="1" />
      <rect x="10" y="4" width="5" height="10" rx="1" />
      <rect x="17" y="4" width="4" height="13" rx="1" />
    </svg>
  );
}

function ClockIcon() {
  return (
    <svg className="h-3.5 w-3.5 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2} aria-hidden="true">
      <circle cx="12" cy="12" r="9" />
      <path strokeLinecap="round" strokeLinejoin="round" d="M12 7v5l3 3" />
    </svg>
  );
}

function BuildingIcon() {
  return (
    <svg className="h-3.5 w-3.5 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2} aria-hidden="true">
      <path strokeLinecap="round" strokeLinejoin="round" d="M4 21V6a1 1 0 011-1h6a1 1 0 011 1v15M15 21v-9a1 1 0 011-1h3a1 1 0 011 1v9M4 21h16M8 9h1m-1 4h1m4-4h1m-1 4h1" />
    </svg>
  );
}

function PaperclipIcon() {
  return (
    <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2} aria-hidden="true">
      <path strokeLinecap="round" strokeLinejoin="round" d="M21.44 11.05l-9.19 9.19a5 5 0 01-7.07-7.07l9.19-9.19a3.5 3.5 0 014.95 4.95l-9.2 9.19a2 2 0 01-2.83-2.83l8.49-8.48" />
    </svg>
  );
}

function CommentIcon() {
  return (
    <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2} aria-hidden="true">
      <path strokeLinecap="round" strokeLinejoin="round" d="M21 11.5a8.38 8.38 0 01-.9 3.8 8.5 8.5 0 01-7.6 4.7 8.38 8.38 0 01-3.8-.9L3 21l1.9-5.7a8.38 8.38 0 01-.9-3.8 8.5 8.5 0 014.7-7.6 8.38 8.38 0 013.8-.9h.5a8.48 8.48 0 018 8v.5z" />
    </svg>
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
