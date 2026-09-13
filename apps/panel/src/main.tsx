import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { BrowserRouter } from "react-router";
import { QueryClientProvider } from "@tanstack/react-query";
import { AuthProvider, ErrorBoundary, TenantProvider, ToastViewport } from "@peoplise/ui";
import { authAdapter } from "@peoplise/api-client";
import { App } from "./App";
import { queryClient } from "./lib/queryClient";
import "./lib/i18n";
import "./styles/index.css";

const rootElement = document.getElementById("root");
if (!rootElement) throw new Error("Missing #root element.");

createRoot(rootElement).render(
  <StrictMode>
    <ErrorBoundary>
      <QueryClientProvider client={queryClient}>
        <TenantProvider>
          <AuthProvider adapter={authAdapter}>
            <BrowserRouter>
              <App />
            </BrowserRouter>
            <ToastViewport />
          </AuthProvider>
        </TenantProvider>
      </QueryClientProvider>
    </ErrorBoundary>
  </StrictMode>,
);
