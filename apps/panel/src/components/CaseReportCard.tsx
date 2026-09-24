import { useTranslation } from "react-i18next";
import { Button, Card, CardHeader, CardTitle, useToast } from "@peoplise/ui";
import { toApiError } from "@peoplise/api-client";
import { useCaseForCandidate, useCaseReport, useResendCaseLink } from "../hooks/useCases";

interface CaseReportCardProps {
  candidateId: string;
  positionId: string;
}

/**
 * VideoInterview report for one candidate's application — a two-step fetch (find the
 * case, then its report), same reasoning as ConversationTranscriptCard: ATS's
 * CandidateProcess and VideoInterview's Case are linked only by sharing a candidateId,
 * never a direct foreign key across modules. A 409 Case.NotCompleted from the report
 * fetch is a normal "interview still in progress" state, not a real error.
 */
export function CaseReportCard({ candidateId, positionId }: CaseReportCardProps) {
  const { t } = useTranslation();
  const { show } = useToast();
  const lookup = useCaseForCandidate(candidateId, positionId);
  const caseId = lookup.data?.caseId ?? null;
  const report = useCaseReport(caseId);
  const reportError = report.error ? toApiError(report.error) : null;
  const isInProgress = reportError?.title === "Case.NotCompleted";
  const resendLink = useResendCaseLink();

  const onResend = async () => {
    try {
      await resendLink.mutateAsync({ candidateId, positionId });
      show(t("candidateDetail.linkResent") as string, "success");
    } catch (error) {
      show(toApiError(error).title, "error");
    }
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle>{t("candidateDetail.videoAnswer")}</CardTitle>
        {caseId && (
          <Button variant="secondary" size="sm" onClick={onResend} disabled={resendLink.isPending}>
            {t("candidateDetail.resendLink")}
          </Button>
        )}
      </CardHeader>

      {(lookup.isLoading || (caseId && report.isLoading)) && (
        <p className="text-sm text-slate-500">{t("common.loading")}</p>
      )}

      {!lookup.isLoading && !caseId && <p className="text-sm text-slate-400">{t("candidateDetail.noCase")}</p>}

      {isInProgress && <p className="text-sm text-slate-400">{t("candidateDetail.caseInProgress")}</p>}

      {reportError && !isInProgress && <p className="text-sm text-red-600">{reportError.title}</p>}

      {report.data && (
        <ul className="flex flex-col gap-3">
          {report.data.sections.map((section) => (
            <li key={section.title}>
              <p className="text-xs font-medium uppercase text-slate-500">{section.title}</p>
              <p className="whitespace-pre-line text-sm text-slate-900">{section.content || t("common.noResults")}</p>
            </li>
          ))}
        </ul>
      )}
    </Card>
  );
}
