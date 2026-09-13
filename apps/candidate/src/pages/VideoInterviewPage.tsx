import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { Button } from "@peoplise/ui";
import { useCameraPreview } from "../hooks/useCameraPreview";

const PREPARATION_SECONDS = 10;
const RECORDING_SECONDS = 90;
const RETAKES_ALLOWED = 1;

type Phase = "preview" | "preparing" | "recording" | "review";

export function VideoInterviewPage() {
  const { t } = useTranslation();
  const { videoRef, status, errorMessage } = useCameraPreview();
  const [phase, setPhase] = useState<Phase>("preview");
  const [secondsLeft, setSecondsLeft] = useState(PREPARATION_SECONDS);
  const [retakesUsed, setRetakesUsed] = useState(0);

  useEffect(() => {
    if (phase !== "preparing" && phase !== "recording") return;

    const timer = setInterval(() => {
      setSecondsLeft((current) => {
        if (current > 1) return current - 1;

        if (phase === "preparing") {
          setPhase("recording");
          return RECORDING_SECONDS;
        }

        setPhase("review");
        return 0;
      });
    }, 1000);

    return () => clearInterval(timer);
  }, [phase]);

  const startPreparation = () => {
    setSecondsLeft(PREPARATION_SECONDS);
    setPhase("preparing");
  };

  const stopRecording = () => setPhase("review");

  const retake = () => {
    setRetakesUsed((count) => count + 1);
    setPhase("preview");
  };

  const retakesRemaining = RETAKES_ALLOWED - retakesUsed;

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
          <Button onClick={startPreparation} disabled={status !== "ready"}>
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
              <Button>{t("common.submit")}</Button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
