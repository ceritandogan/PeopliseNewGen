import { useState } from "react";
import { useTranslation } from "react-i18next";
import { cn } from "@peoplise/ui";

export type SurveyQuestion =
  | { type: "multipleChoice"; id: string; prompt: string; options: string[] }
  | { type: "matrix"; id: string; prompt: string; rows: string[]; scale: string[] }
  | { type: "ranking"; id: string; prompt: string; items: string[] }
  | { type: "fillInTheBlank"; id: string; prompt: string };

export type SurveyAnswer =
  | { type: "multipleChoice"; value: string }
  | { type: "matrix"; value: Record<string, string> }
  | { type: "ranking"; value: string[] }
  | { type: "fillInTheBlank"; value: string };

export interface SurveyQuestionProps {
  question: SurveyQuestion;
  onAnswer: (answer: SurveyAnswer) => void;
}

export function SurveyQuestionCard({ question, onAnswer }: SurveyQuestionProps) {
  const { t } = useTranslation();

  return (
    <fieldset className="flex flex-col gap-4">
      <legend className="text-base font-medium text-slate-900">{question.prompt}</legend>

      {question.type === "multipleChoice" && <MultipleChoice question={question} onAnswer={onAnswer} />}
      {question.type === "matrix" && <Matrix question={question} onAnswer={onAnswer} />}
      {question.type === "ranking" && <Ranking question={question} onAnswer={onAnswer} />}
      {question.type === "fillInTheBlank" && <FillInTheBlank question={question} onAnswer={onAnswer} />}

      <p className="text-xs uppercase tracking-wide text-slate-400">{t(`survey.${question.type}`)}</p>
    </fieldset>
  );
}

function MultipleChoice({
  question,
  onAnswer,
}: {
  question: Extract<SurveyQuestion, { type: "multipleChoice" }>;
  onAnswer: (answer: SurveyAnswer) => void;
}) {
  return (
    <div role="radiogroup" className="flex flex-col gap-2">
      {question.options.map((option) => (
        <label
          key={option}
          className="flex cursor-pointer items-center gap-2 rounded-md border border-slate-200 p-3 text-sm hover:bg-slate-50"
        >
          <input
            type="radio"
            name={question.id}
            value={option}
            onChange={() => onAnswer({ type: "multipleChoice", value: option })}
            className="h-4 w-4"
          />
          {option}
        </label>
      ))}
    </div>
  );
}

function Matrix({
  question,
  onAnswer,
}: {
  question: Extract<SurveyQuestion, { type: "matrix" }>;
  onAnswer: (answer: SurveyAnswer) => void;
}) {
  const [values, setValues] = useState<Record<string, string>>({});

  const setValue = (row: string, scaleValue: string) => {
    const next = { ...values, [row]: scaleValue };
    setValues(next);
    onAnswer({ type: "matrix", value: next });
  };

  return (
    <div className="overflow-x-auto">
      <table className="w-full text-sm">
        <thead>
          <tr>
            <th scope="col" className="text-left" />
            {question.scale.map((scaleValue) => (
              <th key={scaleValue} scope="col" className="px-2 py-1 text-center font-normal text-slate-500">
                {scaleValue}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {question.rows.map((row) => (
            <tr key={row}>
              <th scope="row" className="py-2 text-left font-normal text-slate-800">
                {row}
              </th>
              {question.scale.map((scaleValue) => (
                <td key={scaleValue} className="text-center">
                  <input
                    type="radio"
                    name={`${question.id}-${row}`}
                    aria-label={`${row}: ${scaleValue}`}
                    checked={values[row] === scaleValue}
                    onChange={() => setValue(row, scaleValue)}
                  />
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function Ranking({
  question,
  onAnswer,
}: {
  question: Extract<SurveyQuestion, { type: "ranking" }>;
  onAnswer: (answer: SurveyAnswer) => void;
}) {
  const [order, setOrder] = useState(question.items);

  const move = (index: number, direction: -1 | 1) => {
    const next = [...order];
    const target = index + direction;
    if (target < 0 || target >= next.length) return;
    [next[index], next[target]] = [next[target], next[index]];
    setOrder(next);
    onAnswer({ type: "ranking", value: next });
  };

  return (
    <ol className="flex flex-col gap-2">
      {order.map((item, index) => (
        <li key={item} className="flex items-center justify-between rounded-md border border-slate-200 p-3 text-sm">
          <span>
            {index + 1}. {item}
          </span>
          <span className="flex gap-1">
            <button
              type="button"
              aria-label={`Move ${item} up`}
              onClick={() => move(index, -1)}
              disabled={index === 0}
              className={cn("rounded p-1 hover:bg-slate-100", index === 0 && "opacity-30")}
            >
              ↑
            </button>
            <button
              type="button"
              aria-label={`Move ${item} down`}
              onClick={() => move(index, 1)}
              disabled={index === order.length - 1}
              className={cn("rounded p-1 hover:bg-slate-100", index === order.length - 1 && "opacity-30")}
            >
              ↓
            </button>
          </span>
        </li>
      ))}
    </ol>
  );
}

function FillInTheBlank({
  question,
  onAnswer,
}: {
  question: Extract<SurveyQuestion, { type: "fillInTheBlank" }>;
  onAnswer: (answer: SurveyAnswer) => void;
}) {
  return (
    <input
      type="text"
      aria-label={question.prompt}
      onChange={(event) => onAnswer({ type: "fillInTheBlank", value: event.target.value })}
      className="h-10 rounded-md border border-slate-300 px-3 text-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500"
    />
  );
}
