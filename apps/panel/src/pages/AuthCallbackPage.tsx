import { useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router";
import { useTranslation } from "react-i18next";
import { Button, useAuth } from "@peoplise/ui";

/**
 * Where `/connect/authorize` sends the browser back to with `?code=&state=` (or
 * `?error=`) once the user has authenticated at the backend's login page. Completes the
 * PKCE exchange and lands the user where they were originally headed. See ADR 0002.
 */
export function AuthCallbackPage() {
  const { t } = useTranslation();
  const { completeLogin } = useAuth();
  const navigate = useNavigate();
  const [error, setError] = useState<string | null>(null);
  const hasRun = useRef(false);

  useEffect(() => {
    // StrictMode double-invokes effects in development; this exchange is one-shot
    // (the code and stashed PKCE state are consumed/cleared on first use), so a second
    // run would just fail — guard against running it twice.
    if (hasRun.current) return;
    hasRun.current = true;

    completeLogin(new URLSearchParams(window.location.search))
      .then((returnTo) => navigate(returnTo, { replace: true }))
      .catch((e: unknown) => setError(e instanceof Error ? e.message : String(e)));
  }, [completeLogin, navigate]);

  if (error) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-slate-50 px-4">
        <div className="w-full max-w-sm rounded-lg border border-slate-200 bg-white p-6 text-center shadow-sm">
          <p className="mb-4 text-sm text-red-700">{error}</p>
          <Button onClick={() => navigate("/login", { replace: true })} className="w-full">
            {t("auth.signIn")}
          </Button>
        </div>
      </div>
    );
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-50 px-4">
      <p className="text-sm text-slate-500">{t("auth.signingIn")}</p>
    </div>
  );
}
