import { Route, Routes } from "react-router";
import { BotChatPage } from "./pages/BotChatPage";
import { VideoInterviewPage } from "./pages/VideoInterviewPage";
import { SurveyPage } from "./pages/SurveyPage";

export function App() {
  return (
    <Routes>
      <Route path="/" element={<BotChatPage />} />
      <Route path="/video-interview" element={<VideoInterviewPage />} />
      <Route path="/survey" element={<SurveyPage />} />
    </Routes>
  );
}
