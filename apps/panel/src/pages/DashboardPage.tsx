import { useTranslation } from "react-i18next";
import { Link } from "react-router";
import { Card, Badge, cn } from "@peoplise/ui";
import { usePositionsList } from "../hooks/usePositions";
import { avatarColorFor } from "../lib/avatarColor";

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

      <Card className="flex items-center gap-4">
        <div className="flex h-12 w-12 shrink-0 items-center justify-center rounded-xl bg-brand-100 text-xl">📊</div>
        <div>
          <p className="text-sm font-medium text-slate-500">{t("dashboard.totalApplicants")}</p>
          <p className="text-3xl font-bold text-slate-900">{totalApplicants}</p>
        </div>
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
              <Card key={position.positionId} className="transition-shadow hover:shadow-md">
                <div className="flex items-start justify-between gap-3">
                  <div className="flex min-w-0 items-center gap-3">
                    <div
                      className={cn(
                        "flex h-10 w-10 shrink-0 items-center justify-center rounded-lg text-sm font-semibold",
                        avatarColorFor(position.department || position.title),
                      )}
                    >
                      {(position.department || position.title).slice(0, 1).toUpperCase()}
                    </div>
                    <div className="min-w-0">
                      <Link
                        to={`/positions/${position.positionId}`}
                        className="block truncate font-semibold text-slate-900 hover:text-brand-600 hover:underline"
                      >
                        {position.title}
                      </Link>
                      <p className="truncate text-sm text-slate-500">{position.department}</p>
                    </div>
                  </div>
                  <Badge variant="brand" className="shrink-0">
                    {position.applicantCount}
                  </Badge>
                </div>
              </Card>
            ))}
          </div>
        )}
      </section>
    </div>
  );
}
