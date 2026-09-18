import { useEffect, useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import { useLocation, useParams } from "react-router";
import { Button, Input, useToast } from "@peoplise/ui";
import { conversationsApi, toApiError, type ConversationStepContent } from "@peoplise/api-client";
import { ChatBubble, type ChatMessage } from "../components/ChatBubble";
import { useSendConversationResponse, useStartConversation } from "../hooks/useConversation";

function toChatMessage(step: ConversationStepContent): ChatMessage {
  return {
    id: crypto.randomUUID(),
    from: "bot",
    text: step.content,
    quickReplies: step.quickReplyOptions.length > 0 ? step.quickReplyOptions : undefined,
  };
}

export function BotChatPage() {
  const { t } = useTranslation();
  const { show } = useToast();
  const { positionId } = useParams<{ positionId: string }>();
  const { state } = useLocation();
  const candidateId = (state as { candidateId?: string } | null)?.candidateId;

  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [draft, setDraft] = useState("");
  const [conversationId, setConversationId] = useState<string>();
  const [isEnded, setIsEnded] = useState(false);

  const startConversation = useStartConversation();
  const sendResponse = useSendConversationResponse(conversationId ?? "");
  const hasStarted = useRef(false);

  useEffect(() => {
    if (hasStarted.current || !positionId || !candidateId) return;
    hasStarted.current = true;

    startConversation.mutate(
      { positionId, candidateId, interface: "WebChat" },
      {
        onSuccess: (result) => {
          setConversationId(result.conversationId);
          setMessages([toChatMessage(result.currentStep)]);
          if (result.currentStep.isFinalStep) setIsEnded(true);
        },
        onError: (error) => show(toApiError(error).title, "error"),
      },
    );
  }, [positionId, candidateId, startConversation, show]);

  const respond = async (response: string | null) => {
    try {
      const result = await sendResponse.mutateAsync(response);
      if (!result.currentStep) {
        setIsEnded(true);
        return;
      }

      setMessages((prev) => [...prev, toChatMessage(result.currentStep!)]);
      if (result.currentStep.isFinalStep && conversationId) {
        setIsEnded(true);
        // The engine only closes a conversation once a *next* response is processed for
        // a final step (see ConversationProcessor's remarks) — this one carries no
        // further UI content, it just formally ends what's already shown. Called
        // directly rather than through the sendResponse mutation hook a second time:
        // there's no pending/error UI tied to it, and TanStack Query's mutation
        // observer doesn't handle two mutateAsync calls in quick succession on the same
        // hook instance cleanly.
        await conversationsApi.sendConversationResponse(conversationId, null);
      }
    } catch (error) {
      show(toApiError(error).title, "error");
    }
  };

  const appendCandidateMessage = (text: string) => {
    setMessages((prev) => [...prev, { id: crypto.randomUUID(), from: "candidate", text }]);
    respond(text);
  };

  const onSubmit = (event: React.FormEvent) => {
    event.preventDefault();
    if (!draft.trim()) return;
    appendCandidateMessage(draft.trim());
    setDraft("");
  };

  if (!positionId || !candidateId) {
    return <p className="mx-auto mt-8 max-w-lg text-sm text-slate-500">{t("apply.missingPosition")}</p>;
  }

  return (
    <div className="mx-auto flex h-dvh max-w-lg flex-col">
      <header className="border-b border-slate-100 px-4 py-3">
        <h1 className="text-base font-semibold text-slate-900">{t("common.appName")}</h1>
      </header>

      <div role="log" aria-live="polite" className="flex flex-1 flex-col gap-4 overflow-y-auto px-4 py-4">
        {messages.map((message) => (
          <ChatBubble key={message.id} message={message} onQuickReply={appendCandidateMessage} />
        ))}
        {startConversation.isPending && <p className="text-sm text-slate-400">{t("bot.thinking")}</p>}
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
          disabled={isEnded || !conversationId}
          onChange={(event) => setDraft(event.target.value)}
        />
        <Button type="submit" disabled={isEnded || !conversationId || !draft.trim()}>
          {t("bot.send")}
        </Button>
      </form>
    </div>
  );
}
