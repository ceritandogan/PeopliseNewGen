import { useEffect, useRef, useState } from "react";

export interface CameraPreviewState {
  videoRef: React.RefObject<HTMLVideoElement | null>;
  /** The raw stream, for a recorder (e.g. useMediaRecorder) to capture from — a ref, not state, since the stream object itself never needs to trigger a re-render. */
  streamRef: React.RefObject<MediaStream | null>;
  status: "requesting" | "ready" | "denied" | "unsupported";
  errorMessage: string | null;
}

/** Requests camera + microphone access and streams it into a <video> for a live self-preview — real getUserMedia, not mocked. */
export function useCameraPreview(): CameraPreviewState {
  const videoRef = useRef<HTMLVideoElement>(null);
  const streamRef = useRef<MediaStream | null>(null);
  const [status, setStatus] = useState<CameraPreviewState["status"]>("requesting");
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  useEffect(() => {
    if (!navigator.mediaDevices?.getUserMedia) {
      setStatus("unsupported");
      return;
    }

    let cancelled = false;

    navigator.mediaDevices
      .getUserMedia({ video: true, audio: true })
      .then((mediaStream) => {
        if (cancelled) {
          mediaStream.getTracks().forEach((track) => track.stop());
          return;
        }
        streamRef.current = mediaStream;
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
      streamRef.current?.getTracks().forEach((track) => track.stop());
      streamRef.current = null;
    };
  }, []);

  return { videoRef, streamRef, status, errorMessage };
}
