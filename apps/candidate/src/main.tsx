import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { BrowserRouter } from "react-router";
import { QueryClientProvider } from "@tanstack/react-query";
import { ErrorBoundary, TenantProvider, ToastViewport } from "@peoplise/ui";
import { App } from "./App";
import { queryClient } from "./lib/queryClient";
import "./lib/i18n";
import "./styles/index.css";

// No AuthProvider here, unlike the panel app: a candidate reaches this app via a link
// sent to them ("Adaya gönderilen link ile tarayıcıda açılan sohbet penceresi"), not a
// username/password sign-in. TODO(design): that link needs its own auth mechanism (a
// one-time token in the URL, attached to requests) — not designed yet, so
// @peoplise/api-client's requests currently go out unauthenticated for this app.

const rootElement = document.getElementById("root");
if (!rootElement) throw new Error("Missing #root element.");

createRoot(rootElement).render(
  <StrictMode>
    <ErrorBoundary>
      <QueryClientProvider client={queryClient}>
        <TenantProvider>
          <BrowserRouter>
            <App />
          </BrowserRouter>
          <ToastViewport />
        </TenantProvider>
      </QueryClientProvider>
    </ErrorBoundary>
  </StrictMode>,
);
