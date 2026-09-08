/**
 * Decodifica o payload de um JWT (sem validar assinatura -- o token ja foi
 * validado pelo backend; aqui so lemos as claims para exibir na UI).
 */
export function decodeJwtPayload<T = Record<string, unknown>>(token: string): T | null {
  try {
    const payload = token.split(".")[1];
    const json = atob(payload.replace(/-/g, "+").replace(/_/g, "/"));
    return JSON.parse(decodeURIComponent(escape(json))) as T;
  } catch {
    return null;
  }
}

const CLAIM_NAME = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";

/** Extrai o nome do usuario a partir do access token (claim ClaimTypes.Name). */
export function extrairNomeDoToken(accessToken: string): string | null {
  const payload = decodeJwtPayload<Record<string, string>>(accessToken);
  return payload?.[CLAIM_NAME] ?? null;
}
