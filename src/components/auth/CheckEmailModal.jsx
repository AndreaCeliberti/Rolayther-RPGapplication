import { Button, Modal, Spinner } from "react-bootstrap";
import { useResendConfirmation } from "../../hooks/useResendConfirmation";
import "../../styles/email-flow.css";

// Finestra mostrata subito dopo la registrazione: spiega cosa fare con l'email appena arrivata.
//   email      indirizzo a cui è stato scritto
//   emailSent  false = account creato ma l'email NON è partita (si può chiedere un nuovo invio da qui)
//   isMaster   true = richiesta Master: va ricordato che serve anche l'approvazione di un amministratore
export default function CheckEmailModal({ show, handleClose, email, emailSent, isMaster, openLogin }) {
  const { resend, sending, cooldown } = useResendConfirmation();

  const goToLogin = () => {
    handleClose();
    openLogin?.();
  };

  return (
    <Modal show={show} onHide={handleClose} centered animation contentClassName="border-0 shadow">
      <Modal.Body className="text-center p-4 p-md-5">
        <div className={`flow-icon mx-auto mb-3 ${emailSent === false ? "is-warn" : ""}`}>
          <img src="/email/ico-confirm.png" alt="" />
        </div>

        <h4 className="text-white mb-2">Controlla la tua email</h4>

        {emailSent === false ? (
          <div className="flow-alert small mb-3" role="alert">
            L'account è stato creato, ma <strong>non siamo riusciti a spedire l'email di conferma</strong> a{" "}
            <span className="text-break">{email}</span>. Premi «Reinvia email» qui sotto per riprovare.
          </div>
        ) : (
          <p style={{ color: "var(--muted)" }}>
            Abbiamo inviato un link di conferma a{" "}
            <strong className="text-white text-break">{email}</strong>.
          </p>
        )}

        {/* ogni passo è dentro uno <span>: l'elemento della lista è un riquadro flessibile (numero + testo) */}
        <ol className="flow-steps">
          <li>
            <span>Apri l'email «Conferma la tua email per entrare in Rolayther».</span>
          </li>
          <li>
            <span>
              Premi il pulsante <strong className="text-white">Conferma la mia email</strong>.
            </span>
          </li>
          <li>
            <span>Torna qui e accedi: da quel momento puoi usare tutto il sito.</span>
          </li>
        </ol>

        {isMaster && (
          <p className="small" style={{ color: "var(--muted)" }}>
            La tua richiesta Master sarà valutata da un amministratore: l'esito, con la spiegazione, ti arriverà
            per email.
          </p>
        )}

        <p className="small mb-4" style={{ color: "var(--muted)" }}>
          Non arriva? Guarda nella cartella spam. Il link resta valido per qualche giorno.
        </p>

        <div className="d-grid gap-2">
          <Button onClick={goToLogin}>Vai al login</Button>
          <Button
            variant="outline-secondary"
            size="sm"
            disabled={sending || cooldown > 0}
            onClick={() => resend(email)}
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
        </div>
      </Modal.Body>
    </Modal>
  );
}
