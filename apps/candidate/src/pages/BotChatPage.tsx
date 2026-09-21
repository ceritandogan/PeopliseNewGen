import { useEffect, useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import { useLocation, useParams, useSearchParams } from "react-router";
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
  const [searchParams, setSearchParams] = useSearchParams();

  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [draft, setDraft] = useState("");
  const [conversationId, setConversationId] = useState<string>();
  const [candidateToken, setCandidateToken] = useState<string>();
  const [isEnded, setIsEnded] = useState(false);

  const startConversation = useStartConversation();
  const sendResponse = useSendConversationResponse(conversationId ?? "", candidateToken ?? "");
  const hasStarted = useRef(false);

  useEffect(() => {
    if (hasStarted.current || !positionId) return;
    hasStarted.current = true;

    // A conversationId+token already in the URL means this page load *is* the
    // "candidate returns via their link" case (see ADR 0004) — the token proves who
    // they are, so nothing needs starting again. Rehydrating the visible transcript
    // from history is a separate, not-yet-built feature (ADR 0004's Consequences); the
    // candidate can still continue the conversation from here, they just won't see
    // their prior messages replayed.
    const existingConversationId = searchParams.get("conversationId");
    const existingToken = searchParams.get("token");
    if (existingConversationId && existingToken) {
      setConversationId(existingConversationId);
      setCandidateToken(existingToken);
      return;
    }

    if (!candidateId) return;

    startConversation.mutate(
      { positionId, candidateId, interface: "WebChat" },
      {
        onSuccess: (result) => {
          setConversationId(result.conversation.conversationId);
          setCandidateToken(result.candidateToken);
          setMessages([toChatMessage(result.conversation.currentStep)]);
          if (result.conversation.currentStep.isFinalStep) setIsEnded(true);

          // Puts the token in the URL, per ADR 0004 — this page is now a link that
          // works if reopened (the token still proves who it belongs to), not just a
          // one-time in-session state.
          setSearchParams(
            { conversationId: result.conversation.conversationId, token: result.candidateToken },
            { replace: true },
          );
        },
        onError: (error) => show(toApiError(error).title, "error"),
      },
    );
  }, [positionId, candidateId, startConversation, show, searchParams, setSearchParams]);

  const respond = async (response: string | null) => {
    try {
      const result = await sendResponse.mutateAsync(response);
      if (!result.currentStep) {
        setIsEnded(true);
        return;
      }

      setMessages((prev) => [...prev, toChatMessage(result.currentStep!)]);
      if (result.currentStep.isFinalStep && conversationId && candidateToken) {
        setIsEnded(true);
        // The engine only closes a conversation once a *next* response is processed for
        // a final step (see ConversationProcessor's remarks) — this one carries no
        // further UI content, it just formally ends what's already shown. Called
        // directly rather than through the sendResponse mutation hook a second time:
        // there's no pending/error UI tied to it, and TanStack Query's mutation
        // observer doesn't handle two mutateAsync calls in quick succession on the same
        // hook instance cleanly.
        await conversationsApi.sendConversationResponse(conversationId, null, candidateToken);
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

  // Missing everything needed to proceed: no position, and neither a fresh candidateId
  // (from Apply) nor a conversationId+token already in the URL (a returning candidate).
  if (!positionId || (!candidateId && !conversationId)) {
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
