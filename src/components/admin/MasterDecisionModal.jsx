import { useState } from "react";
import { Button, Form, Modal, Spinner } from "react-bootstrap";
import "../../styles/email-flow.css";

// Finestra con cui l'Admin decide su una richiesta Master. Il testo scritto qui finisce NELL'EMAIL al candidato:
//   mode "approve"  messaggio di benvenuto, facoltativo
//   mode "reject"   motivo del rifiuto, obbligatorio (almeno MIN_REASON caratteri)
const MIN_REASON = 10;
const MAX_LEN = 1000;

// Frasi pronte: un clic le inserisce, poi si possono modificare liberamente
const TEMPLATES = {
  approve: [
    {
      label: "Benvenuto",
      text: "Il tuo profilo ci ha convinti: non vediamo l'ora di vedere le tue sessioni al tavolo!",
    },
    {
      label: "Primi passi",
      text: "Completa il profilo con foto e presentazione, poi crea la tua prima sessione: i giocatori potranno iscriversi subito.",
    },
  ],
  reject: [
    {
      label: "Profilo incompleto",
      text: "Il profilo è ancora troppo scarno: aggiungi una presentazione più completa e qualche dettaglio sulle tue esperienze di gioco, poi ripresenta la richiesta.",
    },
    {
      label: "Poca esperienza",
      text: "Per ora non abbiamo trovato abbastanza esperienza come Master. Quando avrai condotto qualche campagna in più, ripresenta pure la richiesta.",
    },
    {
      label: "Dati non verificabili",
      text: "Non siamo riusciti a verificare i dati indicati nella richiesta. Rispondi a questa email con qualche riferimento in più e la riguarderemo.",
    },
  ],
};

// Il modulo sta DENTRO la finestra: quando la finestra si chiude si smonta e il testo si azzera da solo
function DecisionForm({ mode, master, busy, onClose, onConfirm }) {
  const isReject = mode === "reject";
  const [text, setText] = useState("");
  const [touched, setTouched] = useState(false);

  const trimmed = text.trim();
  const tooShort = isReject && trimmed.length < MIN_REASON;

  const submit = (e) => {
    e.preventDefault();
    setTouched(true);
    if (tooShort || busy) return;
    onConfirm(trimmed);
  };

  return (
    <Form onSubmit={submit} noValidate>
      <Modal.Header closeButton={!busy} className="border-1 pb-0">
        <Modal.Title className="fw-semibold text-white">
          {isReject ? "Rifiuta la richiesta" : "Approva la richiesta"}
        </Modal.Title>
      </Modal.Header>

      <Modal.Body className="pt-2">
        <div className="mb-3" style={{ color: "var(--muted)" }}>
          <strong className="text-white">{master.nickName}</strong> · {master.name} {master.surname}
          <div className="small text-break">{master.email}</div>
        </div>

        <Form.Group controlId="master-decision-text">
          <Form.Label className="fw-semibold" style={{ color: "var(--text)" }}>
            {isReject ? "Motivo del rifiuto" : "Messaggio di benvenuto (facoltativo)"}
          </Form.Label>

          <div className="reason-chips mb-2">
            {TEMPLATES[mode].map((t) => (
              <button
                key={t.label}
                type="button"
                className="reason-chip"
                disabled={busy}
                onClick={() => setText(t.text)}
              >
                {t.label}
              </button>
            ))}
          </div>

          <Form.Control
            as="textarea"
            rows={5}
            maxLength={MAX_LEN}
            value={text}
            disabled={busy}
            onChange={(e) => setText(e.target.value)}
            isInvalid={touched && tooShort}
            placeholder={
              isReject
                ? "Spiega con parole tue perché la richiesta non viene approvata: lo leggerà il candidato."
                : "Scrivi due righe di benvenuto: compariranno nell'email."
            }
          />
          <Form.Control.Feedback type="invalid">
            Scrivi il motivo (almeno {MIN_REASON} caratteri): il candidato lo leggerà nell'email.
          </Form.Control.Feedback>

          <div className="d-flex justify-content-between gap-3 small mt-1" style={{ color: "var(--muted)" }}>
            <span>
              {isReject
                ? "Il candidato riceverà un'email con questo motivo."
                : "Il candidato riceverà un'email di benvenuto: se lasci vuoto avrà solo il testo standard."}
            </span>
            <span className="text-nowrap">
              {text.length}/{MAX_LEN}
            </span>
          </div>
        </Form.Group>
      </Modal.Body>

      <Modal.Footer className="border-0">
        <Button variant="outline-secondary" onClick={onClose} disabled={busy}>
          Annulla
        </Button>
        <Button type="submit" variant={isReject ? "danger" : "success"} disabled={busy}>
          {busy ? (
            <>
              <Spinner size="sm" className="me-2" />
              Invio…
            </>
          ) : isReject ? (
            "Rifiuta e invia email"
          ) : (
            "Approva e invia email"
          )}
        </Button>
      </Modal.Footer>
    </Form>
  );
}

export default function MasterDecisionModal({ show, mode, master, busy, onClose, onConfirm }) {
  return (
    <Modal
      show={show}
      onHide={busy ? undefined : onClose}
      centered
      animation
      backdrop="static"
      contentClassName="border-1 shadow"
    >
      {master && (
        <DecisionForm
          key={`${mode}-${master.masterId}`}
          mode={mode}
          master={master}
          busy={busy}
          onClose={onClose}
          onConfirm={onConfirm}
        />
      )}
    </Modal>
  );
}
