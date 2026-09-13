import { useEffect, useRef, useState } from "react";

export interface CameraPreviewState {
  videoRef: React.RefObject<HTMLVideoElement | null>;
  status: "requesting" | "ready" | "denied" | "unsupported";
  errorMessage: string | null;
}

/** Requests camera + microphone access and streams it into a <video> for a live self-preview — real getUserMedia, not mocked. */
export function useCameraPreview(): CameraPreviewState {
  const videoRef = useRef<HTMLVideoElement>(null);
  const [status, setStatus] = useState<CameraPreviewState["status"]>("requesting");
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  useEffect(() => {
    if (!navigator.mediaDevices?.getUserMedia) {
      setStatus("unsupported");
      return;
    }

    let stream: MediaStream | undefined;
    let cancelled = false;

    navigator.mediaDevices
      .getUserMedia({ video: true, audio: true })
      .then((mediaStream) => {
        if (cancelled) {
          mediaStream.getTracks().forEach((track) => track.stop());
          return;
        }
        stream = mediaStream;
        if (videoRef.current) videoRef.current.srcObject = mediaStream;
        setStatus("ready");
      })
      .catch((error: unknown) => {
        if (cancelled) return;
        setStatus("denied");
        setErrorMessage(error instanceof Error ? error.message : "Camera access was denied.");
      });

    return () => {
      cancelled = true;
      stream?.getTracks().forEach((track) => track.stop());
    };
  }, []);

  return { videoRef, status, errorMessage };
}
