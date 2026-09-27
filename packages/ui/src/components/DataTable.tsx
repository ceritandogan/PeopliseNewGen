import { type ReactNode } from "react";
import { cn } from "../lib/cn";

export interface DataTableColumn<TRow> {
  key: string;
  header: string;
  render: (row: TRow) => ReactNode;
  className?: string;
}

export interface DataTableProps<TRow> {
  columns: DataTableColumn<TRow>[];
  rows: TRow[];
  getRowKey: (row: TRow) => string;
  emptyMessage?: string;
  isLoading?: boolean;
  onRowClick?: (row: TRow) => void;
}

/** A generic, semantic <table> — real <thead>/<tbody>/<th scope="col"> markup, not a div grid, so screen readers and keyboard nav work without extra ARIA plumbing. */
export function DataTable<TRow>({
  columns,
  rows,
  getRowKey,
  emptyMessage = "No results found.",
  isLoading = false,
  onRowClick,
}: DataTableProps<TRow>) {
  return (
    <div className="overflow-x-auto rounded-xl border border-slate-100 shadow-sm">
      <table className="w-full min-w-full divide-y divide-slate-100 text-sm">
        <thead className="bg-slate-50/80">
          <tr>
            {columns.map((column) => (
              <th
                key={column.key}
                scope="col"
                className="px-4 py-3 text-left text-xs font-medium uppercase tracking-wide text-slate-500"
              >
                {column.header}
              </th>
            ))}
          </tr>
        </thead>
        <tbody className="divide-y divide-slate-100 bg-white">
          {isLoading ? (
            <tr>
              <td colSpan={columns.length} className="px-4 py-6 text-center text-slate-500">
                Loading…
              </td>
            </tr>
          ) : rows.length === 0 ? (
            <tr>
              <td colSpan={columns.length} className="px-4 py-6 text-center text-slate-500">
                {emptyMessage}
              </td>
            </tr>
          ) : (
            rows.map((row) => (
              <tr
                key={getRowKey(row)}
                onClick={onRowClick ? () => onRowClick(row) : undefined}
                tabIndex={onRowClick ? 0 : undefined}
                onKeyDown={
                  onRowClick
                    ? (event) => {
                        if (event.key === "Enter" || event.key === " ") {
                          event.preventDefault();
                          onRowClick(row);
                        }
                      }
                    : undefined
                }
                className={cn(onRowClick && "cursor-pointer hover:bg-slate-50 focus-visible:bg-slate-50")}
              >
                {columns.map((column) => (
                  <td key={column.key} className={cn("px-4 py-3 text-slate-800", column.className)}>
                    {column.render(row)}
                  </td>
                ))}
              </tr>
            ))
          )}
        </tbody>
      </table>
    </div>
  );
}
