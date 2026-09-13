import { Navigate, Outlet, useLocation } from "react-router";
import { useAuth } from "../context/AuthContext";

export interface ProtectedRouteProps {
  /** If given, the signed-in user must have at least one of these roles — otherwise they're redirected to `forbiddenPath`. */
  requiredRoles?: string[];
  loginPath?: string;
  forbiddenPath?: string;
}

/**
 * "Protected Route: Rol bazlı sayfa erişim kontrolü." Renders its nested routes
 * (`<Outlet />`) only when authenticated and role-authorized; otherwise redirects,
 * preserving the attempted location so login can send the user back afterward.
 */
export function ProtectedRoute({
  requiredRoles,
  loginPath = "/login",
  forbiddenPath = "/forbidden",
}: ProtectedRouteProps) {
  const { isAuthenticated, session } = useAuth();
  const location = useLocation();

  if (!isAuthenticated) {
    return <Navigate to={loginPath} state={{ from: location }} replace />;
  }

  if (requiredRoles && requiredRoles.length > 0) {
    const hasRequiredRole = session!.user.roles.some((role) => requiredRoles.includes(role));
    if (!hasRequiredRole) {
      return <Navigate to={forbiddenPath} replace />;
    }
  }

  return <Outlet />;
}
