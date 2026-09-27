import { useState } from "react";
import { Link } from "react-router";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useTranslation } from "react-i18next";
import { Button, Input, useAuth, useToast } from "@peoplise/ui";
import { toApiError } from "@peoplise/api-client";
import { useCreateTenant } from "../hooks/useTenants";

const signupSchema = z
  .object({
    workspaceName: z.string().min(1),
    displayName: z.string().min(1),
    email: z.string().email(),
    password: z.string().min(8),
    confirmPassword: z.string().min(8),
  })
  .refine((values) => values.password === values.confirmPassword, {
    path: ["confirmPassword"],
    message: "passwordMismatch",
  });

type SignupForm = z.infer<typeof signupSchema>;

/**
 * Public, unauthenticated by design — this is what creates the very first user of a
 * brand-new tenant, so there's no session yet to gate it behind. On success it does NOT
 * mint a session itself: it redirects into the same `beginLogin()` OAuth Authorization
 * Code + PKCE flow every other sign-in uses (see ADR 0007), so there is only ever one
 * code path that issues a valid session.
 */
export function SignupPage() {
  const { t } = useTranslation();
  const { beginLogin } = useAuth();
  const { show } = useToast();
  const createTenant = useCreateTenant();
  const [isRedirecting, setIsRedirecting] = useState(false);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<SignupForm>({ resolver: zodResolver(signupSchema) });

  const onSubmit = handleSubmit(async (values) => {
    try {
      await createTenant.mutateAsync({
        workspaceName: values.workspaceName,
        displayName: values.displayName,
        email: values.email,
        password: values.password,
      });
      setIsRedirecting(true);
      await beginLogin("/");
    } catch (error) {
      setIsRedirecting(false);
      show(toApiError(error).title, "error");
    }
  });

  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-50 px-4">
      <div className="w-full max-w-sm rounded-xl border border-slate-100 bg-white p-6 shadow-sm">
        <h1 className="mb-1 text-lg font-semibold text-slate-900">{t("signup.title")}</h1>
        <p className="mb-4 text-sm text-slate-500">{t("signup.subtitle")}</p>

        <form onSubmit={onSubmit} className="flex flex-col gap-3" noValidate>
          <Input
            label={t("signup.workspaceName") as string}
            error={errors.workspaceName?.message}
            {...register("workspaceName")}
          />
          <Input
            label={t("signup.displayName") as string}
            error={errors.displayName?.message}
            {...register("displayName")}
          />
          <Input
            label={t("auth.email") as string}
            type="email"
            error={errors.email?.message}
            {...register("email")}
          />
          <Input
            label={t("auth.password") as string}
            type="password"
            autoComplete="new-password"
            error={errors.password?.message}
            {...register("password")}
          />
          <Input
            label={t("signup.confirmPassword") as string}
            type="password"
            autoComplete="new-password"
            error={
              errors.confirmPassword?.message === "passwordMismatch"
                ? t("signup.passwordMismatch")
                : errors.confirmPassword?.message
            }
            {...register("confirmPassword")}
          />

          <Button type="submit" disabled={isSubmitting || isRedirecting} className="mt-2 w-full">
            {isRedirecting ? t("signup.submitting") : t("signup.submit")}
          </Button>
        </form>

        <p className="mt-4 text-center text-sm text-slate-500">
          {t("signup.alreadyHaveAccount")}{" "}
          <Link to="/login" className="text-brand-600 hover:underline">
            {t("auth.signIn")}
          </Link>
        </p>
      </div>
    </div>
  );
}
