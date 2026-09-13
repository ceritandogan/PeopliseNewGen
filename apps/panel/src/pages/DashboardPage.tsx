import { useTranslation } from "react-i18next";
import { Card, CardHeader, CardTitle, Badge } from "@peoplise/ui";

/**
 * TODO(backend): ATS's Application layer only has GetPositionDashboardQuery (one
 * position at a time) — there's no "list every open position" query yet, which this
 * page's card grid needs. Using placeholder data until that query (and its controller
 * route) exist; swap `MOCK_POSITIONS` for a real `usePositionsList()` hook then.
 */
const MOCK_POSITIONS = [
  { id: "1", title: "Senior Backend Engineer", department: "Engineering", applicants: 24 },
  { id: "2", title: "Product Designer", department: "Design", applicants: 12 },
  { id: "3", title: "Customer Success Manager", department: "Operations", applicants: 8 },
];

export function DashboardPage() {
  const { t } = useTranslation();
  const totalApplicants = MOCK_POSITIONS.reduce((sum, position) => sum + position.applicants, 0);

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

      <section aria-labelledby="open-positions-heading" className="flex flex-col gap-3">
        <h2 id="open-positions-heading" className="text-sm font-medium uppercase tracking-wide text-slate-500">
          {t("dashboard.openPositions")}
        </h2>
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {MOCK_POSITIONS.map((position) => (
            <Card key={position.id}>
              <CardHeader>
                <CardTitle>{position.title}</CardTitle>
                <Badge variant="brand">{position.applicants}</Badge>
              </CardHeader>
              <p className="text-sm text-slate-500">{position.department}</p>
            </Card>
          ))}
        </div>
      </section>
    </div>
  );
}
