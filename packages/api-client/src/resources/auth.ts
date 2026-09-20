import type { AuthAdapter } from "@peoplise/ui";
import { beginLogin, completeLogin, logout, refreshTokenGrant } from "../tokenEndpoint";

/** The concrete `AuthAdapter` `AuthProvider` (from `@peoplise/ui`) needs — wire it in at the app root: `<AuthProvider adapter={authAdapter}>`. */
export const authAdapter: AuthAdapter = {
  beginLogin,
  completeLogin,
  refresh: refreshTokenGrant,
  logout,
};
