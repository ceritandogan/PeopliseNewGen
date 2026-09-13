import { useState } from "react";
import { useTranslation } from "react-i18next";
import { Button, Input } from "@peoplise/ui";
import { ChatBubble, type ChatMessage } from "../components/ChatBubble";

/**
 * TODO(backend): none of HrBot's three queries (StartConversation, ProcessUserResponse,
 * GetConversationHistory) return the *current* step's prompt/quick-reply content ahead
 * of the candidate answering — only logs of what already happened. There's no "what
 * should I show right now" query yet. Seeding with a scripted opening message so the
 * chat UI has something to render; wire `sendConversationResponse` (already a real
 * call) to drive the next bot message once that query exists.
 */
const INITIAL_MESSAGE: ChatMessage = {
  id: "welcome",
  from: "bot",
  text: "Hi! Thanks for applying. Do you have 5 minutes for a few quick questions?",
  quickReplies: ["Evet", "Hayır"],
};

export function BotChatPage() {
  const { t } = useTranslation();
  const [messages, setMessages] = useState<ChatMessage[]>([INITIAL_MESSAGE]);
  const [draft, setDraft] = useState("");

  const appendCandidateMessage = (text: string) => {
    setMessages((prev) => [...prev, { id: crypto.randomUUID(), from: "candidate", text }]);
    // A real send would call conversationsApi.sendConversationResponse(conversationId, text)
    // here and append the bot's next message from the result once that's available.
  };

  const onSubmit = (event: React.FormEvent) => {
    event.preventDefault();
    if (!draft.trim()) return;
    appendCandidateMessage(draft.trim());
    setDraft("");
  };

  return (
    <div className="mx-auto flex h-dvh max-w-lg flex-col">
      <header className="border-b border-slate-100 px-4 py-3">
        <h1 className="text-base font-semibold text-slate-900">{t("common.appName")}</h1>
      </header>

      <div role="log" aria-live="polite" className="flex flex-1 flex-col gap-4 overflow-y-auto px-4 py-4">
        {messages.map((message) => (
          <ChatBubble key={message.id} message={message} onQuickReply={appendCandidateMessage} />
        ))}
      </div>

      <form onSubmit={onSubmit} className="flex gap-2 border-t border-slate-100 p-3">
        <label htmlFor="chat-input" className="sr-only">
          {t("bot.typeYourAnswer")}
        </label>
        <Input
          id="chat-input"
          className="flex-1"
          placeholder={t("bot.typeYourAnswer") as string}
          value={draft}
          onChange={(event) => setDraft(event.target.value)}
        />
        <Button type="submit" disabled={!draft.trim()}>
          {t("bot.send")}
        </Button>
      </form>
    </div>
  );
}
