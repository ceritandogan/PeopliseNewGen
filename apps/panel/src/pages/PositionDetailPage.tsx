import { useState } from "react";
import { useParams } from "react-router";
import { useTranslation } from "react-i18next";
import { Card, CardHeader, CardTitle, Button, Badge, cn } from "@peoplise/ui";
import { usePositionDashboard } from "../hooks/usePositions";

const TABS = ["overview", "flowEditor"] as const;
type Tab = (typeof TABS)[number];

const WIREFRAME_STAGES = [
  { id: "1", name: "Application Form", type: "InformationForm" },
  { id: "2", name: "Screening Test", type: "ScreeningTest" },
  { id: "3", name: "Video Interview", type: "VideoInterview" },
  { id: "4", name: "Reviewer Approval", type: "ReviewerApproval" },
  { id: "5", name: "Offer", type: "OfferStage" },
];

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
      )}

      {tab === "flowEditor" && (
        <div className="flex flex-col gap-3">
          <p className="text-sm text-slate-500">
            Wireframe only — drag-and-drop reordering isn't wired up yet, per the architecture doc's own framing
            ("sürükle-bırak aşama tasarımcısı <strong>wireframe</strong>").
          </p>
          <div className="flex gap-3 overflow-x-auto pb-2" aria-label="Workflow stages">
            {WIREFRAME_STAGES.map((stage, index) => (
              <div key={stage.id} className="flex items-center gap-3">
                <Card className="w-48 shrink-0 cursor-grab select-none">
                  <p className="text-sm font-medium text-slate-900">{stage.name}</p>
                  <p className="text-xs text-slate-500">{stage.type}</p>
                </Card>
                {index < WIREFRAME_STAGES.length - 1 && <span aria-hidden className="text-slate-300">→</span>}
              </div>
            ))}
          </div>
          <Button variant="outline" className="w-fit">
            {t("positions.addStage")}
          </Button>
        </div>
      )}
    </div>
  );
}
