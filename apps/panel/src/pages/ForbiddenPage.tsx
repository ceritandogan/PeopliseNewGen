import { useTranslation } from "react-i18next";

export function ForbiddenPage() {
  const { t } = useTranslation();
  return (
    <div role="alert" className="flex min-h-screen flex-col items-center justify-center gap-2 text-center">
      <h1 className="text-lg font-semibold text-slate-900">403</h1>
      <p className="text-slate-600">{t("errors.unauthorized")}</p>
    </div>
  );
}
