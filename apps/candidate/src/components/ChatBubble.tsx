import { cn } from "@peoplise/ui";

export interface ChatMessage {
  id: string;
  from: "bot" | "candidate";
  text: string;
  quickReplies?: string[];
  imageUrl?: string;
}

export interface ChatBubbleProps {
  message: ChatMessage;
  onQuickReply?: (value: string) => void;
}

export function ChatBubble({ message, onQuickReply }: ChatBubbleProps) {
  const isBot = message.from === "bot";

  return (
    <div className={cn("flex flex-col gap-2", isBot ? "items-start" : "items-end")}>
      <div
        className={cn(
          "max-w-[85%] rounded-2xl px-4 py-2 text-sm sm:max-w-sm",
          isBot ? "rounded-tl-sm bg-slate-100 text-slate-900" : "rounded-tr-sm bg-brand-600 text-white",
        )}
      >
        {message.imageUrl && (
          <img src={message.imageUrl} alt="" className="mb-2 rounded-lg" />
        )}
        <p>{message.text}</p>
      </div>

      {isBot && message.quickReplies && message.quickReplies.length > 0 && (
        <div role="group" aria-label="Quick replies" className="flex flex-wrap gap-2">
          {message.quickReplies.map((option) => (
            <button
              key={option}
              type="button"
              onClick={() => onQuickReply?.(option)}
              className="rounded-full border border-brand-300 px-3 py-1.5 text-sm text-brand-700 hover:bg-brand-50"
            >
              {option}
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
