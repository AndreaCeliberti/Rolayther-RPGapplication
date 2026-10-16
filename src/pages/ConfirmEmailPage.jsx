import { useContext, useEffect, useRef, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { Button, Card, Container, Form, Spinner } from "react-bootstrap";
import { AuthApi } from "../api/auth.api";
import { AuthContext } from "../context/AuthContext";
import { useResendConfirmation } from "../hooks/useResendConfirmation";
import { apiErrorCode, apiErrorMessage } from "../utils/apiError";
import "../styles/email-flow.css";

// Pagina aperta dal pulsante "Conferma la mia email" dell'email (/confirm-email?token=...).
// Il link NON conferma nulla da solo: è questa pagina, aprendosi in un browser vero, a mandare il token al server.
// Così le anteprime automatiche dei programmi di posta (antivirus, "link scanner") non confermano al posto dell'utente.

// Pulsante/riquadro per chiedere un nuovo link (link scaduto, rovinato o mancante)
function ResendForm({ defaultEmail }) {
  const [email, setEmail] = useState(defaultEmail || "");
  const { resend, sending, cooldown } = useResendConfirmation();
  const valid = /^\S+@\S+\.\S+$/.test(email.trim());

  const submit = (e) => {
    e.preventDefault();
    if (valid) resend(email.trim());
  };

  return (
    <Form onSubmit={submit} className="text-start mt-3">
      <Form.Label className="small" style={{ color: "var(--muted)" }}>
        La tua email
      </Form.Label>
      <Form.Control
        type="email"
        value={email}
        onChange={(e) => setEmail(e.target.value)}
        placeholder="nome@esempio.it"
        autoComplete="email"
        required
      />
      <Button type="submit" className="w-100 mt-2" disabled={!valid || sending || cooldown > 0}>
        {sending ? (
          <>
            <Spinner size="sm" className="me-2" />
            Invio…
          </>
        ) : cooldown > 0 ? (
          `Riprova tra ${cooldown}s`
        ) : (
          "Inviami un nuovo link"
        )}
      </Button>
    </Form>
  );
}

function StatusCard({ tone, icon, title, children }) {
  return (
    <Container className="px-3">
      <Card className="flow-card border-0 text-center">
        <Card.Body className="p-4 p-md-5">
          <div className={`flow-icon mx-auto mb-3 ${tone ? `is-${tone}` : ""}`}>
            {icon ? <img src={icon} alt="" /> : <Spinner animation="border" variant="light" />}
          </div>
          <h2 className="h4 text-white mb-2">{title}</h2>
          <div style={{ color: "var(--muted)" }}>{children}</div>
        </Card.Body>
      </Card>
    </Container>
  );
}

function ConfirmEmailInner({ token, openLogin }) {
  const { user, refreshEmailStatus } = useContext(AuthContext);
  const [result, setResult] = useState({ status: token ? "loading" : "missing" });

  // In sviluppo React esegue gli effetti due volte: la richiesta parte una volta sola (altrimenti la seconda
  // risponderebbe "già confermato" e l'utente non vedrebbe mai il messaggio di successo)
  const startedRef = useRef(false);
  const mountedRef = useRef(true);

  useEffect(() => {
    mountedRef.current = true;
    return () => {
      mountedRef.current = false;
    };
  }, []);

  useEffect(() => {
    if (!token || startedRef.current) return;
    startedRef.current = true;

    AuthApi.confirmEmail(token)
      .then((res) => {
        if (!mountedRef.current) return;
        setResult({ status: res.data?.status === "already-confirmed" ? "already" : "confirmed" });
        refreshEmailStatus(); // se l'utente è collegato, l'avviso in alto sparisce
      })
      .catch((err) => {
        if (!mountedRef.current) return;

        const code = apiErrorCode(err);
        const httpStatus = err?.response?.status;
        if (code === "TokenExpired") setResult({ status: "expired" });
        // 400 senza codice = il server ha rifiutato il contenuto stesso del link (troppo lungo, spezzato...)
        else if (code === "TokenInvalid" || httpStatus === 400) setResult({ status: "invalid" });
        else if (httpStatus === 429)
          setResult({ status: "error", message: "Troppi tentativi ravvicinati: aspetta un minuto e riprova." });
        else
          setResult({
            status: "error",
            message: apiErrorMessage(err, "Non riesco a contattare il server. Riprova tra poco."),
          });
      });
  }, [token, refreshEmailStatus]);

  // dopo la conferma: chi è già collegato va a vedere le sessioni, gli altri possono accedere
  const nextStep = user ? (
    <Link to="/sessions" className="btn btn-primary mt-3">
      Vai alle sessioni
    </Link>
  ) : (
    <Button className="mt-3" onClick={() => openLogin?.()}>
      Accedi a Rolayther
    </Button>
  );

  switch (result.status) {
    case "loading":
      return (
        <StatusCard title="Sto confermando la tua email…">
          Un attimo, ci vuole solo un secondo.
        </StatusCard>
      );

    case "confirmed":
      return (
        <StatusCard tone="success" icon="/email/ico-confirm.png" title="Email confermata!">
          Grazie: l'indirizzo è verificato. Ora puoi iscriverti alle sessioni e, se sei un Master approvato,
          crearle.
          <div>{nextStep}</div>
        </StatusCard>
      );

    case "already":
      return (
        <StatusCard tone="success" icon="/email/ico-confirm.png" title="Email già confermata">
          Questo indirizzo risultava già verificato: non devi fare altro.
          <div>{nextStep}</div>
        </StatusCard>
      );

    case "expired":
      return (
        <StatusCard tone="warn" icon="/email/ico-confirm.png" title="Il link è scaduto">
          Per sicurezza i link di conferma durano solo pochi giorni. Chiedine uno nuovo: ti arriva in pochi
          istanti.
          <ResendForm defaultEmail={user?.email} />
        </StatusCard>
      );

    case "invalid":
      return (
        <StatusCard tone="error" icon="/email/ico-rejected.png" title="Il link non è valido">
          Il link è incompleto o è stato modificato (a volte il programma di posta lo spezza su due righe).
          Copialo per intero oppure chiedine uno nuovo.
          <ResendForm defaultEmail={user?.email} />
        </StatusCard>
      );

    case "missing":
      return (
        <StatusCard tone="warn" icon="/email/ico-confirm.png" title="Manca il link di conferma">
          Questa pagina si apre dal pulsante «Conferma la mia email» dell'email che ti abbiamo scritto. Non
          l'hai più? Chiedine una nuova.
          <ResendForm defaultEmail={user?.email} />
        </StatusCard>
      );

    default:
      return (
        <StatusCard tone="error" icon="/email/ico-rejected.png" title="Conferma non riuscita">
          {result.message}
          <div>
            <Button className="mt-3" variant="outline-secondary" onClick={() => window.location.reload()}>
              Riprova
            </Button>
          </div>
        </StatusCard>
      );
  }
}

// Il token fa da chiave: se cambia (altro link aperto nella stessa scheda) la pagina riparte da zero
export default function ConfirmEmailPage({ openLogin }) {
  const [params] = useSearchParams();
  const token = params.get("token") || "";

  return <ConfirmEmailInner key={token} token={token} openLogin={openLogin} />;
}
