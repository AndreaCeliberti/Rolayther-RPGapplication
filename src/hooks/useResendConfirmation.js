import { useCallback, useContext, useEffect, useState } from "react";
import { AuthApi } from "../api/auth.api";
import { ToastContext } from "../context/ToastContext";
import { apiErrorMessage } from "../utils/apiError";

// "Reinvia l'email di conferma", con conto alla rovescia tra un invio e l'altro.
// Lo usano il banner sotto la barra in alto, la finestra "Controlla la tua email" e la pagina di conferma.
//
//   const { resend, sending, cooldown } = useResendConfirmation();
//   <Button disabled={sending || cooldown > 0} onClick={() => resend(email)}>
export function useResendConfirmation() {
  const { showToast } = useContext(ToastContext);
  const [sending, setSending] = useState(false);
  const [cooldown, setCooldown] = useState(0); // secondi mancanti al prossimo invio possibile

  useEffect(() => {
    if (cooldown <= 0) return undefined;
    const timer = setTimeout(() => setCooldown((c) => c - 1), 1000);
    return () => clearTimeout(timer);
  }, [cooldown]);

  // true = richiesta accolta (la mail parte in background). Il server risponde allo stesso modo
  // anche per indirizzi sconosciuti o già confermati: per questo il messaggio dice "se risulta...".
  const resend = useCallback(
    async (email) => {
      if (!email) return false;

      setSending(true);
      try {
        const res = await AuthApi.resendConfirmation(email);
        setCooldown(Number(res.data?.cooldownSeconds) || 60);
        showToast(
          "Se l'indirizzo risulta registrato e non ancora confermato, a breve riceverai una nuova email. Controlla anche lo spam.",
          "success",
          8000
        );
        return true;
      } catch (err) {
        if (err?.response?.status === 429) {
          setCooldown(60);
          showToast("Troppe richieste ravvicinate: riprova tra un minuto.", "warning", 5000);
        } else {
          showToast(
            apiErrorMessage(err, "Non riesco a inviare l'email in questo momento. Riprova più tardi."),
            "danger",
            5000
          );
        }
        return false;
      } finally {
        setSending(false);
      }
    },
    [showToast]
  );

  return { resend, sending, cooldown };
}
