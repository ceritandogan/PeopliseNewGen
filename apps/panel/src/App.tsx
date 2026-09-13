import { Route, Routes } from "react-router";
import { ProtectedRoute } from "@peoplise/ui";
import { AppLayout } from "./layouts/AppLayout";
import { DashboardPage } from "./pages/DashboardPage";
import { PositionsListPage } from "./pages/PositionsListPage";
import { PositionDetailPage } from "./pages/PositionDetailPage";
import { CandidatePipelinePage } from "./pages/CandidatePipelinePage";
import { CandidateDetailPage } from "./pages/CandidateDetailPage";
import { LoginPage } from "./pages/LoginPage";
import { ForbiddenPage } from "./pages/ForbiddenPage";
import { NotFoundPage } from "./pages/NotFoundPage";

export function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/forbidden" element={<ForbiddenPage />} />

      <Route element={<ProtectedRoute />}>
        <Route element={<AppLayout />}>
          <Route index element={<DashboardPage />} />
          <Route path="positions" element={<PositionsListPage />} />
          <Route path="positions/:positionId" element={<PositionDetailPage />} />
          <Route path="positions/:positionId/pipeline" element={<CandidatePipelinePage />} />
          <Route path="candidates/:candidateProcessId" element={<CandidateDetailPage />} />
        </Route>
      </Route>

      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  );
}
