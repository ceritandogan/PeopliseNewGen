import { type ReactNode, useEffect, useRef } from "react";
import { cn } from "../lib/cn";

export interface ModalProps {
  open: boolean;
  onClose: () => void;
  title: string;
  children: ReactNode;
  className?: string;
}

/**
 * Built on the native <dialog> element deliberately: it gives us focus trapping,
 * Escape-to-close, and a backdrop for free, which a hand-rolled div-based modal would
 * otherwise need to reimplement to meet the a11y bar the architecture doc asks for.
 */
export function Modal({ open, onClose, title, children, className }: ModalProps) {
  const dialogRef = useRef<HTMLDialogElement>(null);

  useEffect(() => {
    const dialog = dialogRef.current;
    if (!dialog) return;

    if (open && !dialog.open) dialog.showModal();
    if (!open && dialog.open) dialog.close();
  }, [open]);

  return (
    <dialog
      ref={dialogRef}
      aria-labelledby="modal-title"
      onClose={onClose}
      onCancel={onClose}
      className={cn(
        "w-full max-w-md rounded-lg border border-slate-200 bg-white p-0 shadow-xl backdrop:bg-slate-900/50",
        className,
      )}
    >
      <div className="flex items-center justify-between border-b border-slate-200 px-4 py-3">
        <h2 id="modal-title" className="text-base font-semibold text-slate-900">
          {title}
        </h2>
        <button
          type="button"
          aria-label="Close"
          onClick={onClose}
          className="rounded-md p-1 text-slate-500 hover:bg-slate-100 hover:text-slate-900"
        >
          ✕
        </button>
      </div>
      <div className="p-4">{children}</div>
    </dialog>
  );
}
