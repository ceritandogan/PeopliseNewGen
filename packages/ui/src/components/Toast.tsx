import { useToastStore } from "../stores/toastStore";
import { cn } from "../lib/cn";

const VARIANT_STYLES: Record<string, string> = {
  info: "bg-slate-900 text-white",
  success: "bg-emerald-600 text-white",
  error: "bg-red-600 text-white",
};

/** Mount once near the app root. Reads directly from the Zustand store — call `useToastStore.getState().show(...)` from anywhere, no provider/context needed. */
export function ToastViewport() {
  const toasts = useToastStore((state) => state.toasts);
  const dismiss = useToastStore((state) => state.dismiss);

  return (
    <div
      role="region"
      aria-live="polite"
      aria-label="Notifications"
      className="pointer-events-none fixed inset-x-0 bottom-4 z-50 flex flex-col items-center gap-2 px-4"
    >
      {toasts.map((toast) => (
        <div
          key={toast.id}
          role="status"
          className={cn(
            "pointer-events-auto flex w-full max-w-sm items-center justify-between gap-3 rounded-md px-4 py-3 text-sm shadow-lg",
            VARIANT_STYLES[toast.variant],
          )}
        >
          <span>{toast.message}</span>
          <button
            type="button"
            aria-label="Dismiss"
            onClick={() => dismiss(toast.id)}
            className="shrink-0 opacity-80 hover:opacity-100"
          >
            ✕
          </button>
        </div>
      ))}
    </div>
  );
}

/** Convenience hook so components don't need to know this is a Zustand store under the hood. */
export function useToast() {
  const show = useToastStore((state) => state.show);
  return { show };
}
