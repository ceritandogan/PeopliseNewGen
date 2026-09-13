import { useState } from "react";
import { useTranslation } from "react-i18next";
import { Button } from "@peoplise/ui";
import { SurveyQuestionCard, type SurveyAnswer, type SurveyQuestion } from "../components/SurveyQuestion";

/**
 * TODO(backend): same gap as the bot chat — no query exposes a survey's question list
 * ahead of time. Sample questions cover the renderer types the architecture doc asks
 * for (multiple-choice, matrix, ranking, fill-in-the-blank); swap for a real fetch once
 * that capability exists on the backend.
 */
const QUESTIONS: SurveyQuestion[] = [
  {
    type: "multipleChoice",
    id: "q1",
    prompt: "How many years of professional experience do you have?",
    options: ["0-1", "1-3", "3-5", "5+"],
  },
  {
    type: "matrix",
    id: "q2",
    prompt: "Rate your comfort level with each:",
    rows: ["React", "TypeScript", "SQL"],
    scale: ["1", "2", "3", "4", "5"],
  },
  {
    type: "ranking",
    id: "q3",
    prompt: "Rank these by priority for you:",
    items: ["Salary", "Remote work", "Growth opportunities", "Team culture"],
  },
  {
    type: "fillInTheBlank",
    id: "q4",
    prompt: "What's the capital of your current country of residence?",
  },
];

export function SurveyPage() {
  const { t } = useTranslation();
  const [index, setIndex] = useState(0);
  const [answers, setAnswers] = useState<Record<string, SurveyAnswer>>({});

  const question = QUESTIONS[index];
  const isLast = index === QUESTIONS.length - 1;
  const hasAnswer = Boolean(answers[question.id]);

  const next = () => {
    if (isLast) return;
    setIndex((current) => current + 1);
  };

  return (
    <div className="mx-auto flex min-h-dvh max-w-lg flex-col gap-6 p-4">
      <header>
        <h1 className="text-base font-semibold text-slate-900">{t("survey.title")}</h1>
        <p className="text-sm text-slate-500">{t("survey.questionOf", { current: index + 1, total: QUESTIONS.length })}</p>
      </header>

      <SurveyQuestionCard
        key={question.id}
        question={question}
        onAnswer={(answer) => setAnswers((prev) => ({ ...prev, [question.id]: answer }))}
      />

      <Button onClick={next} disabled={!hasAnswer || isLast} className="mt-auto">
        {isLast ? t("survey.submitAnswer") : t("common.next")}
      </Button>
    </div>
  );
}
