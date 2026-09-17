import { useTranslation } from "react-i18next";
import { Card, CardHeader, CardTitle, Badge } from "@peoplise/ui";
import { usePositionsList } from "../hooks/usePositions";

/**
 * No position lifecycle/status exists yet (see CONTEXT.md), so this lists every
 * position for the tenant rather than filtering to some notion of "open". A single
 * generous page covers the whole list without pagination UI — see the dashboard's
 * design notes for why the total-applicants card relies on that.
 */
const DASHBOARD_PAGE_SIZE = 200;

export function DashboardPage() {
  const { t } = useTranslation();
  const { data, isLoading } = usePositionsList({ pageSize: DASHBOARD_PAGE_SIZE });
  const positions = data?.items ?? [];
  const totalApplicants = positions.reduce((sum, position) => sum + position.applicantCount, 0);

  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-xl font-semibold text-slate-900">{t("dashboard.title")}</h1>

      <Card>
        <CardHeader>
          <CardTitle>{t("dashboard.candidateDistribution")}</CardTitle>
        </CardHeader>
        <p className="text-3xl font-bold text-brand-700">{totalApplicants}</p>
        <p className="text-sm text-slate-500">{t("dashboard.totalApplicants")}</p>
      </Card>

      <section aria-labelledby="positions-heading" className="flex flex-col gap-3">
        <h2 id="positions-heading" className="text-sm font-medium uppercase tracking-wide text-slate-500">
          {t("dashboard.positions")}
        </h2>
        {isLoading ? (
          <p className="text-sm text-slate-500">{t("common.loading")}</p>
        ) : positions.length === 0 ? (
          <p className="text-sm text-slate-500">{t("common.noResults")}</p>
        ) : (
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {positions.map((position) => (
              <Card key={position.positionId}>
                <CardHeader>
                  <CardTitle>{position.title}</CardTitle>
                  <Badge variant="brand">{position.applicantCount}</Badge>
                </CardHeader>
                <p className="text-sm text-slate-500">{position.department}</p>
              </Card>
            ))}
          </div>
        )}
      </section>
    </div>
  );
}
