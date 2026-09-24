import { Route, Routes } from "react-router";
import { ProtectedRoute } from "@peoplise/ui";
import { AppLayout } from "./layouts/AppLayout";
import { DashboardPage } from "./pages/DashboardPage";
import { PositionsListPage } from "./pages/PositionsListPage";
import { PositionDetailPage } from "./pages/PositionDetailPage";
import { CandidatePipelinePage } from "./pages/CandidatePipelinePage";
import { CandidateDetailPage } from "./pages/CandidateDetailPage";
import { CandidateComparisonPage } from "./pages/CandidateComparisonPage";
import { CaseFlowEditorPage } from "./pages/CaseFlowEditorPage";
import { BotFlowEditorPage } from "./pages/BotFlowEditorPage";
import { CompetencyEditorPage } from "./pages/CompetencyEditorPage";
import { LoginPage } from "./pages/LoginPage";
import { AuthCallbackPage } from "./pages/AuthCallbackPage";
import { ForbiddenPage } from "./pages/ForbiddenPage";
import { NotFoundPage } from "./pages/NotFoundPage";

export function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/auth/callback" element={<AuthCallbackPage />} />
      <Route path="/forbidden" element={<ForbiddenPage />} />

      <Route element={<ProtectedRoute />}>
        <Route element={<AppLayout />}>
          <Route index element={<DashboardPage />} />
          <Route path="positions" element={<PositionsListPage />} />
          <Route path="positions/:positionId" element={<PositionDetailPage />} />
          <Route path="positions/:positionId/pipeline" element={<CandidatePipelinePage />} />
          <Route
            path="positions/:positionId/case-bot-projects/:caseBotProjectId/comparison"
            element={<CandidateComparisonPage />}
          />
          <Route
            path="positions/:positionId/case-bot-projects/:caseBotProjectId/flows"
            element={<CaseFlowEditorPage />}
          />
          <Route
            path="positions/:positionId/bot-projects/:botProjectId/flows"
            element={<BotFlowEditorPage />}
          />
          <Route
            path="positions/:positionId/case-bot-projects/:caseBotProjectId/competencies"
            element={<CompetencyEditorPage />}
          />
          <Route path="candidates/:candidateProcessId" element={<CandidateDetailPage />} />
        </Route>
      </Route>

      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  );
}
