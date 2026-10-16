import api from "./axios";

// Conferma dell'indirizzo email (route backend: /api/Auth/...)
export const AuthApi = {
  // token = quello del link ricevuto per email (/confirm-email?token=...).
  // Esiti: 200 { status: "confirmed" | "already-confirmed" } oppure 400 { code: "TokenExpired" | "TokenInvalid" }
  confirmEmail: (token) => api.post("/Auth/confirm-email", { token }),

  // Chiede un nuovo link. La risposta è sempre 200 { message, cooldownSeconds }, che l'indirizzo esista o no
  // (così nessuno può scoprire chi è registrato). Troppe richieste ravvicinate: 429.
  resendConfirmation: (email) => api.post("/Auth/resend-confirmation", { email }),

  // Stato attuale per l'utente collegato: { emailConfirmed }
  emailStatus: () => api.get("/Auth/email-status"),
};
