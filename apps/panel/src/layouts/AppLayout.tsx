import { NavLink, Outlet } from "react-router";
import { useTranslation } from "react-i18next";
import { useAuth, useTenant, useThemeStore, cn } from "@peoplise/ui";
import { supportedLanguages } from "@peoplise/i18n";

const NAV_ITEMS = [
  { to: "/", labelKey: "nav.dashboard" },
  { to: "/positions", labelKey: "nav.positions" },
] as const;

export function AppLayout() {
  const { t, i18n } = useTranslation();
  const { logout, session } = useAuth();
  const { tenantSlug } = useTenant();
  const toggleTheme = useThemeStore((state) => state.toggleTheme);

  return (
    <div className="flex min-h-screen">
      <aside className="hidden w-56 shrink-0 border-r border-slate-200 bg-white md:block" aria-label="Primary">
        <div className="flex h-14 items-center px-4 text-lg font-semibold text-brand-700">{t("common.appName")}</div>
        <nav>
          <ul className="flex flex-col gap-1 p-2">
            {NAV_ITEMS.map((item) => (
              <li key={item.to}>
                <NavLink
                  to={item.to}
                  end={item.to === "/"}
                  className={({ isActive }) =>
                    cn(
                      "block rounded-md px-3 py-2 text-sm font-medium text-slate-700 hover:bg-slate-100",
                      isActive && "bg-brand-50 text-brand-700",
                    )
                  }
                >
                  {t(item.labelKey)}
                </NavLink>
              </li>
            ))}
          </ul>
        </nav>
      </aside>

      <div className="flex min-w-0 flex-1 flex-col">
        <header className="flex h-14 items-center justify-between border-b border-slate-200 bg-white px-4">
          <div className="flex items-center gap-2 text-sm text-slate-600">
            <label htmlFor="tenant-select" className="sr-only">
              {t("header.selectTenant")}
            </label>
            <select id="tenant-select" defaultValue={tenantSlug ?? ""} className="rounded-md border border-slate-300 px-2 py-1 text-sm">
              <option value="" disabled>
                {t("header.selectTenant")}
              </option>
              {tenantSlug && <option value={tenantSlug}>{tenantSlug}</option>}
            </select>
          </div>

          <div className="flex items-center gap-3">
            <label htmlFor="language-select" className="sr-only">
              {t("header.language")}
            </label>
            <select
              id="language-select"
              value={i18n.language}
              onChange={(event) => void i18n.changeLanguage(event.target.value)}
              className="rounded-md border border-slate-300 px-2 py-1 text-sm"
            >
              {supportedLanguages.map((lang) => (
                <option key={lang} value={lang}>
                  {lang.toUpperCase()}
                </option>
              ))}
            </select>

            <button
              type="button"
              onClick={toggleTheme}
              aria-label="Toggle theme"
              className="rounded-md p-1.5 text-slate-500 hover:bg-slate-100"
            >
              🌓
            </button>

            <div className="flex items-center gap-2 text-sm text-slate-700">
              <span aria-label={t("header.profile")}>{session?.user.email}</span>
              <button type="button" onClick={logout} className="text-brand-600 hover:underline">
                {t("auth.signOut")}
              </button>
            </div>
          </div>
        </header>

        <main className="flex-1 p-6">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
