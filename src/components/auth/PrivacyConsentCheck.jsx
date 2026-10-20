import { Form } from "react-bootstrap";
import { Link } from "react-router-dom";
import { PRIVACY_PATH } from "../../config/privacy";
import "../../styles/privacy.css";

// Casella obbligatoria delle finestre di registrazione: «Ho letto l'informativa e acconsento».
// Il link apre l'informativa in una nuova scheda, così il modulo già compilato non si perde.
// invalid: si accende dopo un tentativo di invio senza consenso (o se lo si toglie dopo).
export default function PrivacyConsentCheck({ id, checked, onChange, invalid = false, disabled = false }) {
  const errorId = `${id}-errore`;

  return (
    <>
      <Form.Check
        type="checkbox"
        id={id}
        name="privacyConsent"
        className="privacy-consent"
        checked={checked}
        onChange={(e) => onChange(e.target.checked)}
        isInvalid={invalid}
        aria-invalid={invalid ? "true" : undefined}
        aria-describedby={invalid ? errorId : undefined}
        disabled={disabled}
        required
        label={
          <>
            Ho letto l’
            <Link to={PRIVACY_PATH} target="_blank" rel="noopener noreferrer">
              informativa sulla privacy
              <span className="visually-hidden"> (si apre in una nuova scheda)</span>
            </Link>{" "}
            e acconsento al trattamento dei miei dati personali per usare Rolayther.
          </>
        }
      />
      {invalid && (
        <div id={errorId} className="privacy-consent-error" role="alert">
          Per registrarti serve il tuo consenso.
        </div>
      )}
    </>
  );
}
