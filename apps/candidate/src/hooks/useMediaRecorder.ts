import { useCallback, useRef, useState } from "react";

/** Captures a video Blob from a live stream (e.g. useCameraPreview's streamRef) — real MediaRecorder, not mocked. */
export function useMediaRecorder(streamRef: React.RefObject<MediaStream | null>) {
  const recorderRef = useRef<MediaRecorder | null>(null);
  const chunksRef = useRef<Blob[]>([]);
  const [isRecording, setIsRecording] = useState(false);

  const start = useCallback(() => {
    const stream = streamRef.current;
    if (!stream) return;

    chunksRef.current = [];
    const recorder = new MediaRecorder(stream, { mimeType: "video/webm" });
    recorder.ondataavailable = (event) => {
      if (event.data.size > 0) chunksRef.current.push(event.data);
    };
    recorder.start();
    recorderRef.current = recorder;
    setIsRecording(true);
  }, [streamRef]);

  const stop = useCallback((): Promise<Blob> => {
    return new Promise((resolve) => {
      const recorder = recorderRef.current;
      if (!recorder || recorder.state === "inactive") {
        resolve(new Blob(chunksRef.current, { type: "video/webm" }));
        return;
      }

      recorder.onstop = () => {
        setIsRecording(false);
        resolve(new Blob(chunksRef.current, { type: "video/webm" }));
      };
      recorder.stop();
    });
  }, []);

  return { start, stop, isRecording };
}
