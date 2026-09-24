import { useTranslation } from "react-i18next";
import { Badge, Button, Card, CardHeader, CardTitle, useToast } from "@peoplise/ui";
import { toApiError } from "@peoplise/api-client";
import { useConversationForCandidate, useConversationHistory, useResendConversationLink } from "../hooks/useConversation";

interface ConversationTranscriptCardProps {
  candidateId: string;
  positionId: string;
}

/**
 * HrBot transcript for one candidate's application — a two-step fetch (find the
 * conversation, then its history) because ATS's CandidateProcess and HrBot's
 * Conversation are linked only by sharing a candidateId, never a direct foreign key
 * across modules.
 */
export function ConversationTranscriptCard({ candidateId, positionId }: ConversationTranscriptCardProps) {
  const { t } = useTranslation();
  const { show } = useToast();
  const lookup = useConversationForCandidate(candidateId, positionId);
  const conversationId = lookup.data?.conversationId ?? null;
  const history = useConversationHistory(conversationId);
  const resendLink = useResendConversationLink();

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
        <div className="flex items-center gap-2">
          <CardTitle>{t("candidateDetail.conversation")}</CardTitle>
          {history.data && <Badge variant="brand">{history.data.status}</Badge>}
        </div>
        {conversationId && (
          <Button variant="secondary" size="sm" onClick={onResend} disabled={resendLink.isPending}>
            {t("candidateDetail.resendLink")}
          </Button>
        )}
      </CardHeader>

      {(lookup.isLoading || (conversationId && history.isLoading)) && (
        <p className="text-sm text-slate-500">{t("common.loading")}</p>
      )}

      {!lookup.isLoading && !conversationId && (
        <p className="text-sm text-slate-400">{t("candidateDetail.noConversation")}</p>
      )}

      {history.data && (
        <div className="flex flex-col gap-4">
          <ol className="flex flex-col gap-3">
            {history.data.logs.map((log, index) => (
              <li key={index} className="flex flex-col gap-1">
                {log.botMessage && (
                  <p className="w-fit max-w-[90%] rounded-lg bg-slate-100 px-3 py-2 text-sm text-slate-800">
                    {log.botMessage}
                  </p>
                )}
                {log.candidateResponse && (
                  <p className="ml-auto w-fit max-w-[90%] rounded-lg bg-brand-50 px-3 py-2 text-sm text-brand-900">
                    {log.candidateResponse}
                  </p>
                )}
                <p className="text-xs text-slate-400">{new Date(log.loggedAt).toLocaleString()}</p>
              </li>
            ))}
            {history.data.logs.length === 0 && <li className="text-sm text-slate-400">{t("common.noResults")}</li>}
          </ol>

          {history.data.variables.length > 0 && (
            <div>
              <p className="mb-2 text-xs font-medium uppercase text-slate-500">{t("candidateDetail.capturedAnswers")}</p>
              <dl className="grid grid-cols-2 gap-2 text-sm">
                {history.data.variables.map((variable) => (
                  <div key={variable.key} className="contents">
                    <dt className="text-slate-500">{variable.key}</dt>
                    <dd className="text-slate-900">{variable.value}</dd>
                  </div>
                ))}
              </dl>
            </div>
          )}
        </div>
      )}
    </Card>
  );
}
