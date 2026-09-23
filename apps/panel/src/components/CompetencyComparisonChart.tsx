import { BarElement, CategoryScale, Chart as ChartJS, Legend, LinearScale, Tooltip } from "chart.js";
import { Bar } from "react-chartjs-2";
import type { CandidateComparisonItem, CandidateNameLookup, Competency } from "@peoplise/api-client";

ChartJS.register(CategoryScale, LinearScale, BarElement, Tooltip, Legend);

// Validated categorical palette (dataviz skill, light mode) — fixed hue order, never
// cycled or reassigned per filter; the adjacent-pairlist gates this order clears cover
// bars specifically. See references/palette.md in the dataviz skill.
const SERIES_COLORS = ["#2a78d6", "#eb6834", "#1baf7a", "#eda100", "#e87ba4", "#008300", "#4a3aa7", "#e34948"];

const CHART_INK = { secondary: "#52514e", muted: "#898781", gridline: "#e1e0d9", baseline: "#c3c2b7" };

export function CompetencyComparisonChart({
  items,
  competencies,
  names,
}: {
  items: CandidateComparisonItem[];
  competencies: Competency[];
  names: Record<string, CandidateNameLookup> | undefined;
}) {
  if (items.length === 0 || competencies.length === 0) return null;

  const labels = items.map((item) => names?.[item.candidateId]?.name ?? item.candidateId);

  const datasets = competencies.map((competency, index) => ({
    label: competency.name,
    data: items.map((item) => item.competencyScores[competency.id] ?? null),
    backgroundColor: SERIES_COLORS[index % SERIES_COLORS.length],
    borderRadius: 4,
    borderSkipped: false as const,
    maxBarThickness: 24,
  }));

  // Bounded competencies per group vs. unbounded candidates: let the chart grow wider
  // (scrolling in its own overflow-x container, same convention DataTable uses) rather
  // than squeezing bars as more candidates are compared.
  const width = Math.max(480, items.length * competencies.length * 44);

  return (
    <div className="overflow-x-auto rounded-lg border border-slate-200 p-4">
      <div style={{ width, height: 320 }}>
        <Bar
          data={{ labels, datasets }}
          options={{
            responsive: true,
            maintainAspectRatio: false,
            scales: {
              x: {
                grid: { display: false },
                border: { color: CHART_INK.baseline },
                ticks: { color: CHART_INK.muted, font: { size: 12 } },
              },
              y: {
                min: 0,
                max: 100,
                ticks: { stepSize: 25, color: CHART_INK.muted, font: { size: 12 } },
                grid: { color: CHART_INK.gridline },
                border: { display: false },
              },
            },
            plugins: {
              legend: {
                position: "bottom",
                labels: { usePointStyle: true, pointStyle: "rect", color: CHART_INK.secondary, font: { size: 13 } },
              },
              tooltip: {
                backgroundColor: "#ffffff",
                titleColor: "#0b0b0b",
                bodyColor: CHART_INK.secondary,
                borderColor: "#e2e8f0",
                borderWidth: 1,
                padding: 10,
                cornerRadius: 6,
                usePointStyle: true,
                callbacks: {
                  label: (context) => `${context.dataset.label}: ${(context.parsed.y as number).toFixed(2)}`,
                },
              },
            },
          }}
        />
      </div>
    </div>
  );
}
