import { useEffect, useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import { useParams, useSearchParams } from "react-router";
import { Button, useToast } from "@peoplise/ui";
import { toApiError } from "@peoplise/api-client";
import { useCameraPreview } from "../hooks/useCameraPreview";
import { useMediaRecorder } from "../hooks/useMediaRecorder";
import { useStartCandidateCase, useSubmitVideoAnswer } from "../hooks/useCase";

type Phase = "preview" | "preparing" | "recording" | "review" | "submitted";

/**
 * Reached via its own direct link (`/video-interview/:positionId`), not chained after
 * the bot chat — there's no stage-orchestration concept anywhere in this codebase yet
 * that would drive "what a candidate does next" (see the grilled Stage 9 plan). Mints
 * its own candidateId the same way ApplyPage does, since it's a standalone entry point.
 */
export function VideoInterviewPage() {
  const { t } = useTranslation();
  const { show } = useToast();
  const { positionId } = useParams<{ positionId: string }>();
  const [candidateId] = useState(() => crypto.randomUUID());
  const [, setSearchParams] = useSearchParams();

  const { videoRef, streamRef, status, errorMessage } = useCameraPreview();
  const mediaRecorder = useMediaRecorder(streamRef);

  const [phase, setPhase] = useState<Phase>("preview");
  const [secondsLeft, setSecondsLeft] = useState(0);
  const [retakesUsed, setRetakesUsed] = useState(0);
  const [recordedBlob, setRecordedBlob] = useState<Blob | null>(null);

  const startCase = useStartCandidateCase();
  const hasStarted = useRef(false);

  useEffect(() => {
    if (hasStarted.current || !positionId) return;
    hasStarted.current = true;

    // Always starts fresh on load, unlike BotChatPage — the in-progress camera/recording
    // state (phase, media stream, recorded blob) can't be resumed from a URL either way,
    // so there's nothing to gain from checking for an existing caseId+token here. The
    // token still goes in the URL below, for consistency with ADR 0004 and so a
    // freshly-started case's link is shareable/reloadable the same way a conversation's is.
    startCase.mutate(
      { positionId, candidateId },
      {
        onSuccess: (result) =>
          setSearchParams({ caseId: result.case.caseId, token: result.candidateToken }, { replace: true }),
        onError: (error) => show(toApiError(error).title, "error"),
      },
    );
  }, [positionId, candidateId, startCase, show, setSearchParams]);

  const caseResult = startCase.data?.case;
  const candidateToken = startCase.data?.candidateToken ?? "";
  const submitAnswer = useSubmitVideoAnswer(caseResult?.caseId ?? "", candidateToken);

  useEffect(() => {
    if (phase !== "preparing" && phase !== "recording") return;

    const timer = setInterval(() => {
      setSecondsLeft((current) => {
        if (current > 1) return current - 1;

        if (phase === "preparing") {
          mediaRecorder.start();
          setPhase("recording");
          return caseResult?.recordingTimeSeconds ?? 90;
        }

        mediaRecorder.stop().then(setRecordedBlob);
        setPhase("review");
        return 0;
      });
    }, 1000);

    return () => clearInterval(timer);
  }, [phase, mediaRecorder, caseResult]);

  const startPreparation = () => {
    setSecondsLeft(caseResult?.preparationTimeSeconds ?? 10);
    setPhase("preparing");
  };

  const stopRecording = () => {
    mediaRecorder.stop().then(setRecordedBlob);
    setPhase("review");
  };

  const retake = () => {
    setRetakesUsed((count) => count + 1);
    setRecordedBlob(null);
    setPhase("preview");
  };

  const submit = async () => {
    if (!caseResult || !recordedBlob) return;
    try {
      const file = new File([recordedBlob], "answer.webm", { type: recordedBlob.type || "video/webm" });
      await submitAnswer.mutateAsync({ stepId: caseResult.stepId, file });
      setPhase("submitted");
    } catch (error) {
      show(toApiError(error).title, "error");
    }
  };

  const retakesRemaining = (caseResult?.retakesAllowed ?? 0) - retakesUsed;

  if (!positionId) {
    return <p className="mx-auto mt-8 max-w-lg text-sm text-slate-500">{t("apply.missingPosition")}</p>;
  }

  if (phase === "submitted") {
    return <p className="mx-auto mt-8 max-w-lg text-sm text-slate-600">{t("videoInterview.submitted")}</p>;
  }

  return (
    <div className="mx-auto flex h-dvh max-w-lg flex-col gap-4 p-4">
      <h1 className="text-base font-semibold text-slate-900">{t("videoInterview.title")}</h1>

      <div className="relative aspect-[3/4] w-full overflow-hidden rounded-xl bg-slate-900 sm:aspect-video">
        {status === "denied" && (
          <p role="alert" className="flex h-full items-center justify-center p-4 text-center text-sm text-white">
            {errorMessage ?? "Camera access was denied."}
          </p>
        )}
        {status === "unsupported" && (
          <p role="alert" className="flex h-full items-center justify-center p-4 text-center text-sm text-white">
            This browser doesn't support camera access.
          </p>
        )}
        <video
          ref={videoRef}
          autoPlay
          playsInline
          muted
          className="h-full w-full object-cover"
          aria-label={t("videoInterview.cameraPreview")}
        />

        {(phase === "preparing" || phase === "recording") && (
          <div
            role="timer"
            aria-live="assertive"
            className="absolute right-3 top-3 rounded-full bg-black/60 px-3 py-1 text-sm font-medium text-white"
          >
            {phase === "preparing" ? t("videoInterview.preparationTime") : t("videoInterview.recordingTime")}:{" "}
            {secondsLeft}s
          </div>
        )}
      </div>

      <div className="flex flex-col items-center gap-2">
        {phase === "preview" && (
          <Button onClick={startPreparation} disabled={status !== "ready" || !caseResult}>
            {t("videoInterview.startRecording")}
          </Button>
        )}
        {phase === "recording" && (
          <Button variant="destructive" onClick={stopRecording}>
            {t("videoInterview.stopRecording")}
          </Button>
        )}
        {phase === "review" && (
          <div className="flex flex-col items-center gap-2">
            <p className="text-sm text-slate-600">
              {retakesRemaining > 0
                ? t("videoInterview.retakesRemaining", { count: retakesRemaining })
                : t("videoInterview.noRetakesLeft")}
            </p>
            <div className="flex gap-2">
              {retakesRemaining > 0 && (
                <Button variant="outline" onClick={retake}>
                  {t("videoInterview.retake")}
                </Button>
              )}
              <Button onClick={submit} disabled={submitAnswer.isPending || !recordedBlob}>
                {submitAnswer.isPending ? t("videoInterview.submitting") : t("common.submit")}
              </Button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
