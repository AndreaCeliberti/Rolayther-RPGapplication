import { useContext, useState } from "react";
import { Button, Container, Spinner } from "react-bootstrap";
import { AuthContext } from "../../context/AuthContext";
import { ToastContext } from "../../context/ToastContext";
import { useResendConfirmation } from "../../hooks/useResendConfirmation";
import "../../styles/email-flow.css";

// Striscia sotto la barra in alto, visibile solo a chi è collegato ma non ha ancora cliccato il link di conferma
// (Player, Master e Master in attesa; l'Admin è escluso). Senza conferma si può navigare e accedere,
// ma non iscriversi alle sessioni né crearle: qui si capisce perché e si può chiedere un nuovo link.
export default function EmailConfirmBanner() {
  const { user, needsEmailConfirmation, refreshEmailStatus } = useContext(AuthContext);
  const { showToast } = useContext(ToastContext);
  const { resend, sending, cooldown } = useResendConfirmation();
  const [checking, setChecking] = useState(false);

  if (!needsEmailConfirmation) return null;

  // "Ho già confermato": chiede al server com'è la situazione (ad esempio se il link è stato aperto da un altro dispositivo)
  const handleCheck = async () => {
    setChecking(true);
    const confirmed = await refreshEmailStatus();
    setChecking(false);

    if (confirmed === true) {
      showToast("Email confermata, grazie! Ora puoi usare tutte le funzioni.", "success", 5000);
    } else if (confirmed === false) {
      showToast("L'email non risulta ancora confermata: apri il link che ti abbiamo scritto.", "warning", 5000);
    } else {
      showToast("Non riesco a controllare in questo momento. Riprova tra poco.", "danger", 4000);
    }
  };

  return (
    <div className="email-banner" role="status">
      <Container className="py-2 d-flex flex-column flex-lg-row align-items-lg-center gap-2 gap-lg-3">
        <div className="d-flex align-items-start align-items-lg-center gap-2 flex-grow-1">
          <span className="email-banner-icon" aria-hidden="true">
            ✉️
          </span>
          <div className="small">
            <strong>Conferma la tua email.</strong> Ti abbiamo scritto a{" "}
            <span className="fw-semibold text-break">{user.email}</span>: apri il link per poterti iscrivere
            alle sessioni (o crearle). Non la trovi? Guarda anche nello spam.
          </div>
        </div>

        <div className="d-flex gap-2 flex-shrink-0">
          <Button
            size="sm"
            variant="warning"
            disabled={sending || cooldown > 0}
            onClick={() => resend(user.email)}
          >
            {sending ? (
              <>
                <Spinner size="sm" className="me-1" />
                Invio…
              </>
            ) : cooldown > 0 ? (
              `Reinvia tra ${cooldown}s`
            ) : (
              "Reinvia email"
            )}
          </Button>

          <Button size="sm" variant="outline-secondary" disabled={checking} onClick={handleCheck}>
            {checking ? "Controllo…" : "Ho già confermato"}
          </Button>
        </div>
      </Container>
    </div>
  );
}
