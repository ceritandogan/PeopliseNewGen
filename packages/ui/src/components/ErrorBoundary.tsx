import { Component, type ErrorInfo, type ReactNode } from "react";
import { Button } from "./Button";

interface ErrorBoundaryProps {
  children: ReactNode;
  fallback?: (error: Error, reset: () => void) => ReactNode;
}

interface ErrorBoundaryState {
  error: Error | null;
}

/**
 * The "Global Error Boundary" the architecture doc's common mechanisms call for.
 * Catches render-time errors anywhere below it in the tree; does not catch errors in
 * event handlers or async code (React's boundary contract) — those go through
 * try/catch + the Toast system instead.
 */
export class ErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  state: ErrorBoundaryState = { error: null };

  static getDerivedStateFromError(error: Error): ErrorBoundaryState {
    return { error };
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    // A real deployment would forward this to an error-tracking service.
    console.error("Unhandled render error:", error, info.componentStack);
  }

  reset = () => this.setState({ error: null });

  render() {
    const { error } = this.state;
    if (!error) return this.props.children;

    if (this.props.fallback) return this.props.fallback(error, this.reset);

    return (
      <div role="alert" className="flex min-h-[50vh] flex-col items-center justify-center gap-4 p-8 text-center">
        <h1 className="text-lg font-semibold text-slate-900">Something went wrong.</h1>
        <p className="max-w-md text-sm text-slate-600">{error.message}</p>
        <Button onClick={this.reset}>Try again</Button>
      </div>
    );
  }
}
