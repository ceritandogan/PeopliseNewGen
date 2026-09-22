import { useState } from "react";
import { useParams } from "react-router";
import { useTranslation } from "react-i18next";
import { Card, CardHeader, CardTitle, Badge, Button, Input, useAuth, useToast } from "@peoplise/ui";
import { toApiError } from "@peoplise/api-client";
import { CaseReportCard } from "../components/CaseReportCard";
import { ConversationTranscriptCard } from "../components/ConversationTranscriptCard";
import { useAddCandidateNote, useCandidateDetail } from "../hooks/useCandidates";

export function CandidateDetailPage() {
  const { t } = useTranslation();
  const { candidateProcessId } = useParams<{ candidateProcessId: string }>();
  const { data, isLoading } = useCandidateDetail(candidateProcessId);
  const addNote = useAddCandidateNote(candidateProcessId ?? "");
  const { session } = useAuth();
  const { show } = useToast();
  const [noteText, setNoteText] = useState("");

  if (isLoading) return <p className="text-sm text-slate-500">{t("common.loading")}</p>;
  if (!data) return <p className="text-sm text-slate-500">{t("common.noResults")}</p>;

  const submitNote = async () => {
    if (!noteText.trim()) return;
    try {
      await addNote.mutateAsync({ authorId: session?.user.email ?? "unknown", text: noteText, isPrivate: false });
      setNoteText("");
      show("Note added.", "success");
    } catch (error) {
      show(toApiError(error).title, "error");
    }
  };

  return (
    <div className="grid grid-cols-1 gap-4 lg:grid-cols-3">
      <div className="flex flex-col gap-4 lg:col-span-2">
        <Card>
          <CardHeader>
            <CardTitle>{t("candidateDetail.profile")}</CardTitle>
            <Badge variant="brand">{data.status}</Badge>
          </CardHeader>
          <dl className="grid grid-cols-2 gap-2 text-sm">
            <dt className="text-slate-500">Name</dt>
            <dd className="text-slate-900">{data.candidateName}</dd>
            <dt className="text-slate-500">Email</dt>
            <dd className="text-slate-900">{data.candidateEmail}</dd>
            {data.candidatePhone && (
              <>
                <dt className="text-slate-500">Phone</dt>
                <dd className="text-slate-900">{data.candidatePhone}</dd>
              </>
            )}
            {data.resumeUrl && (
              <>
                <dt className="text-slate-500">{t("candidateDetail.resume")}</dt>
                <dd>
                  <a href={data.resumeUrl} target="_blank" rel="noreferrer" className="text-brand-600 hover:underline">
                    {data.resumeUrl}
                  </a>
                </dd>
              </>
            )}
          </dl>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>{t("candidateDetail.timeline")}</CardTitle>
          </CardHeader>
          <ol className="flex flex-col gap-3 border-l border-slate-200 pl-4">
            {data.evaluations.map((evaluation, index) => (
              <li key={index} className="relative">
                <span className="absolute -left-[21px] top-1 h-2.5 w-2.5 rounded-full bg-brand-500" aria-hidden />
                <p className="text-sm font-medium text-slate-900">
                  {evaluation.evaluatorId} — {evaluation.score}/100
                </p>
                {evaluation.comments && <p className="text-sm text-slate-600">{evaluation.comments}</p>}
                <p className="text-xs text-slate-400">{new Date(evaluation.submittedAt).toLocaleString()}</p>
              </li>
            ))}
            {data.evaluations.length === 0 && <li className="text-sm text-slate-400">{t("common.noResults")}</li>}
          </ol>
        </Card>
      </div>

      <div className="flex flex-col gap-4">
        <ConversationTranscriptCard candidateId={data.candidateId} positionId={data.positionId} />

        <CaseReportCard candidateId={data.candidateId} positionId={data.positionId} />

        <Card>
          <CardHeader>
            <CardTitle>{t("candidateDetail.notes")}</CardTitle>
          </CardHeader>
          <div className="flex flex-col gap-2">
            {data.notes.map((note, index) => (
              <div key={index} className="rounded-md bg-slate-50 p-2 text-sm">
                <p className="text-slate-800">{note.text}</p>
                <p className="text-xs text-slate-400">
                  {note.authorId} · {new Date(note.createdAt).toLocaleString()}
                  {note.isPrivate && ` · ${t("candidateDetail.privateNote")}`}
                </p>
              </div>
            ))}
            <Input
              label={t("candidateDetail.addNote")}
              value={noteText}
              onChange={(event) => setNoteText(event.target.value)}
            />
            <Button onClick={submitNote} disabled={addNote.isPending || !noteText.trim()} className="w-fit">
              {t("common.add")}
            </Button>
          </div>
        </Card>
      </div>
    </div>
  );
}
