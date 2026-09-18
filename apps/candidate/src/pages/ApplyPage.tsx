import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useNavigate, useParams } from "react-router";
import { useTranslation } from "react-i18next";
import { Button, Input, useToast } from "@peoplise/ui";
import { toApiError } from "@peoplise/api-client";
import { useSubmitApplication } from "../hooks/useApply";

const applySchema = z.object({
  candidateName: z.string().min(1),
  candidateEmail: z.string().email(),
  candidatePhone: z.string().optional(),
  resumeUrl: z.string().url().optional().or(z.literal("")),
});

type ApplyForm = z.infer<typeof applySchema>;

/**
 * Minimal public apply form — the thin slice's candidate-facing entry point. Reached via
 * a link containing the target position's id (`/apply/:positionId`); no login, matching
 * the rest of this app (see main.tsx's remark on the candidate app having no
 * AuthProvider). Candidate identity is client-generated (crypto.randomUUID()) rather
 * than looked up or registered — real candidate accounts/dedup are out of scope for the
 * thin slice (see SubmitCandidateApplicationRequest's candidateId doc comment).
 */
export function ApplyPage() {
  const { t } = useTranslation();
  const { positionId } = useParams<{ positionId: string }>();
  const navigate = useNavigate();
  const { show } = useToast();
  const submitApplication = useSubmitApplication();

  // Generated once per visit (not per submit) so the same id is available both in the
  // request payload and, on success, in the redirect into the bot chat.
  const [candidateId] = useState(() => crypto.randomUUID());

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<ApplyForm>({ resolver: zodResolver(applySchema) });

  const onSubmit = handleSubmit(async (values) => {
    if (!positionId) return;
    try {
      await submitApplication.mutateAsync({
        candidateId,
        positionId,
        candidateName: values.candidateName,
        candidateEmail: values.candidateEmail,
        candidatePhone: values.candidatePhone || undefined,
        resumeUrl: values.resumeUrl || undefined,
      });
      navigate(`/bot-chat/${positionId}`, { state: { candidateId } });
    } catch (error) {
      show(toApiError(error).title, "error");
    }
  });

  if (!positionId) {
    return <p className="mx-auto mt-8 max-w-lg text-sm text-slate-500">{t("apply.missingPosition")}</p>;
  }

  return (
    <div className="mx-auto flex min-h-dvh max-w-lg flex-col gap-6 p-4">
      <header>
        <h1 className="text-base font-semibold text-slate-900">{t("apply.title")}</h1>
      </header>

      <form onSubmit={onSubmit} className="flex flex-col gap-3" noValidate>
        <Input
          label={t("apply.fullName") as string}
          error={errors.candidateName?.message}
          {...register("candidateName")}
        />
        <Input
          label={t("apply.email") as string}
          type="email"
          error={errors.candidateEmail?.message}
          {...register("candidateEmail")}
        />
        <Input label={t("apply.phone") as string} error={errors.candidatePhone?.message} {...register("candidatePhone")} />
        <Input
          label={t("apply.resumeUrl") as string}
          error={errors.resumeUrl?.message}
          {...register("resumeUrl")}
        />
        <Button type="submit" disabled={isSubmitting} className="mt-2">
          {t("apply.submit")}
        </Button>
      </form>
    </div>
  );
}
