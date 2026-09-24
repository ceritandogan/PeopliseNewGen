import { type SelectHTMLAttributes, forwardRef, useId } from "react";
import { cva, type VariantProps } from "class-variance-authority";
import { cn } from "../lib/cn";

const selectVariants = cva(
  "rounded-md border border-slate-300 bg-white text-slate-900 " +
    "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500",
  {
    variants: {
      size: {
        sm: "h-7 px-1.5 text-xs",
        md: "h-10 px-3 text-sm",
      },
    },
    defaultVariants: {
      size: "md",
    },
  },
);

export interface SelectProps
  extends Omit<SelectHTMLAttributes<HTMLSelectElement>, "size">,
    VariantProps<typeof selectVariants> {
  label?: string;
  error?: string;
}

/** A labeled native <select> — same label/error/id shape as Input, with a Button-style size variant for compact inline forms. */
export const Select = forwardRef<HTMLSelectElement, SelectProps>(
  ({ className, label, error, size, id, children, ...props }, ref) => {
    const generatedId = useId();
    const selectId = id ?? generatedId;
    const errorId = error ? `${selectId}-error` : undefined;

    const select = (
      <select
        ref={ref}
        id={label ? selectId : undefined}
        aria-invalid={error ? true : undefined}
        aria-describedby={errorId}
        className={cn(selectVariants({ size }), error && "border-red-500 focus-visible:ring-red-500", className)}
        {...props}
      >
        {children}
      </select>
    );

    if (!label) return select;

    return (
      <div className="flex flex-col gap-1.5">
        <label htmlFor={selectId} className="text-sm font-medium text-slate-700">
          {label}
        </label>
        {select}
        {error && (
          <p id={errorId} role="alert" className="text-sm text-red-600">
            {error}
          </p>
        )}
      </div>
    );
  },
);
Select.displayName = "Select";
