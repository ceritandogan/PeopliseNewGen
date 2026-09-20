import { useState } from "react";
import { useTranslation } from "react-i18next";
import { useLocation, type Location } from "react-router";
import { Button, useAuth, useToast } from "@peoplise/ui";
import { toApiError } from "@peoplise/api-client";

/**
 * No credential form here anymore — signing in means leaving this app entirely for the
 * backend's own `/connect/authorize` → `/connect/login` (see ADR 0002). This page's only
 * job is starting that redirect with the right PKCE state and remembering where to send
 * the user back to.
 */
export function LoginPage() {
  const { t } = useTranslation();
  const { beginLogin } = useAuth();
  const { show } = useToast();
  const location = useLocation() as Location & { state?: { from?: Location } };
  const [isRedirecting, setIsRedirecting] = useState(false);

  const returnTo = location.state?.from ? location.state.from.pathname + location.state.from.search : "/";

  const onSignIn = async () => {
    setIsRedirecting(true);
    try {
      await beginLogin(returnTo);
    } catch (error) {
      setIsRedirecting(false);
      show(toApiError(error).title, "error");
    }
  };

  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-50 px-4">
      <div className="w-full max-w-sm rounded-lg border border-slate-200 bg-white p-6 text-center shadow-sm">
        <h1 className="mb-4 text-lg font-semibold text-slate-900">{t("common.appName")}</h1>
        <Button onClick={onSignIn} disabled={isRedirecting} className="w-full">
          {t("auth.signIn")}
        </Button>
      </div>
    </div>
  );
}
