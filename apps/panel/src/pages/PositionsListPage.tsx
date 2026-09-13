import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useTranslation } from "react-i18next";
import { Link } from "react-router";
import { Button, DataTable, Input, Modal, useToast, type DataTableColumn } from "@peoplise/ui";
import { toApiError } from "@peoplise/api-client";
import { useCreatePosition } from "../hooks/usePositions";

const createPositionSchema = z.object({
  title: z.string().min(1),
  department: z.string().min(1),
  city: z.string().min(1),
  country: z.string().min(1),
  workMode: z.enum(["Office", "Remote", "Hybrid"]),
  seniorityLevel: z.enum(["Intern", "Junior", "Mid", "Senior", "Lead", "Principal"]),
  employmentType: z.enum(["FullTime", "PartTime"]),
});

type CreatePositionForm = z.infer<typeof createPositionSchema>;

interface PositionRow {
  id: string;
  title: string;
  department: string;
}

// Same backend gap as the Dashboard page: no list-positions query yet.
const MOCK_ROWS: PositionRow[] = [
  { id: "1", title: "Senior Backend Engineer", department: "Engineering" },
  { id: "2", title: "Product Designer", department: "Design" },
];

const columns: DataTableColumn<PositionRow>[] = [
  { key: "title", header: "Title", render: (row) => <Link to={`/positions/${row.id}`} className="text-brand-600 hover:underline">{row.title}</Link> },
  { key: "department", header: "Department", render: (row) => row.department },
];

export function PositionsListPage() {
  const { t } = useTranslation();
  const { show } = useToast();
  const [isCreateOpen, setCreateOpen] = useState(false);
  const createPosition = useCreatePosition();

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<CreatePositionForm>({
    resolver: zodResolver(createPositionSchema),
    defaultValues: { workMode: "Hybrid", seniorityLevel: "Mid", employmentType: "FullTime" },
  });

  const onSubmit = handleSubmit(async (values) => {
    try {
      await createPosition.mutateAsync(values);
      show("Position created.", "success");
      reset();
      setCreateOpen(false);
    } catch (error) {
      show(toApiError(error).title, "error");
    }
  });

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center justify-between">
        <h1 className="text-xl font-semibold text-slate-900">{t("positions.title")}</h1>
        <Button onClick={() => setCreateOpen(true)}>{t("positions.newPosition")}</Button>
      </div>

      <DataTable columns={columns} rows={MOCK_ROWS} getRowKey={(row) => row.id} />

      <Modal open={isCreateOpen} onClose={() => setCreateOpen(false)} title={t("positions.newPosition")}>
        <form onSubmit={onSubmit} className="flex flex-col gap-3" noValidate>
          <Input label={t("positions.title") as string} error={errors.title?.message} {...register("title")} />
          <Input label={t("positions.department")} error={errors.department?.message} {...register("department")} />
          <Input label={t("positions.location")} error={errors.city?.message} {...register("city")} />
          <Input label="Country" error={errors.country?.message} {...register("country")} />
          <div className="flex justify-end gap-2 pt-2">
            <Button type="button" variant="outline" onClick={() => setCreateOpen(false)}>
              {t("common.cancel")}
            </Button>
            <Button type="submit" disabled={isSubmitting}>
              {t("common.save")}
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  );
}
