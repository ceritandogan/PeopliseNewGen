import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useTranslation } from "react-i18next";
import { useLocation, useNavigate, type Location } from "react-router";
import { Button, Input, useAuth, useToast } from "@peoplise/ui";
import { toApiError } from "@peoplise/api-client";

const loginSchema = z.object({
  email: z.string().email(),
  password: z.string().min(1),
});

type LoginForm = z.infer<typeof loginSchema>;

export function LoginPage() {
  const { t } = useTranslation();
  const { login } = useAuth();
  const { show } = useToast();
  const navigate = useNavigate();
  const location = useLocation() as Location & { state?: { from?: Location } };

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginForm>({ resolver: zodResolver(loginSchema) });

  const onSubmit = handleSubmit(async ({ email, password }) => {
    try {
      await login(email, password);
      navigate(location.state?.from?.pathname ?? "/", { replace: true });
    } catch (error) {
      show(toApiError(error).title, "error");
    }
  });

  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-50 px-4">
      <form onSubmit={onSubmit} noValidate className="w-full max-w-sm rounded-lg border border-slate-200 bg-white p-6 shadow-sm">
        <h1 className="mb-4 text-lg font-semibold text-slate-900">{t("common.appName")}</h1>
        <div className="flex flex-col gap-3">
          <Input label={t("auth.email")} type="email" autoComplete="username" error={errors.email?.message} {...register("email")} />
          <Input
            label={t("auth.password")}
            type="password"
            autoComplete="current-password"
            error={errors.password?.message}
            {...register("password")}
          />
          <Button type="submit" disabled={isSubmitting} className="mt-2">
            {t("auth.signIn")}
          </Button>
        </div>
      </form>
    </div>
  );
}
