import { Link } from "react-router-dom";
import { Button, Container } from "react-bootstrap";
import TableSeats from "./TableSeats";
import Sparkles from "./Sparkles";
import {
  canJoin,
  dateParts,
  emptySeatLayout,
  pluralize,
  seatLayout,
  seatsLabel,
  seatsTone,
  tableLabel,
} from "../../utils/homeSessions";

// Porta chi clicca alla sezione delle sessioni (e, se il sistema lo chiede, senza animazione)
function scrollToSessions(event) {
  const target = document.getElementById("sessioni");
  if (!target) return;

  event.preventDefault();
  const calm = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
  target.scrollIntoView({ behavior: calm ? "auto" : "smooth", block: "start" });
  target.focus({ preventScroll: true });
}

const capitalize = (text) => text.charAt(0).toUpperCase() + text.slice(1);

function pulseText(sessionsCount, hostCount) {
  const sessions = `${sessionsCount} ${pluralize(sessionsCount, "sessione", "sessioni")} in programma`;
  if (hostCount === 0) return `${sessions}.`;
  return `${sessions} e ${hostCount} Master a dirigerl${sessionsCount === 1 ? "a" : "e"}.`;
}

function memberSentence({ role, mineCount, mineLoading, pendingCount }) {
  if (role === "Admin") {
    if (pendingCount == null) return "Da qui tieni d'occhio le sessioni e le richieste dei nuovi Master.";
    if (pendingCount === 0) return "Nessuna richiesta Master in attesa.";
    return `${pendingCount} ${pluralize(pendingCount, "richiesta aspetta", "richieste aspettano")} una risposta per diventare Master.`;
  }
  if (mineCount == null) return mineLoading ? "Carico le tue sessioni…" : "Ecco cosa c'è in programma.";
  if (role === "Master") {
    return mineCount > 0
      ? `Hai ${mineCount} ${pluralize(mineCount, "sessione", "sessioni")} in programma.`
      : "Non hai ancora sessioni in programma: apri il tuo primo tavolo.";
  }
  return mineCount > 0
    ? `Sei iscritto a ${mineCount} ${pluralize(mineCount, "sessione", "sessioni")} in programma.`
    : "Non sei ancora iscritto a nessuna sessione: scegli un tavolo.";
}

// ---------------------------------------------------------------------------------------------
// La scheda con il tavolo: la prossima sessione, con i suoi posti

function TableCard({ status, featured, isMine, role, onRegisterMaster, onCreateSession, onRetry }) {
  if (status === "loading") {
    return (
      <div className="hm-tablecard" role="group" aria-busy="true" aria-label="Prossima sessione, in caricamento">
        <TableSeats seats={emptySeatLayout(6)} label="Tavolo in caricamento" />
        <div className="hm-skel hm-skel--title" />
        <div className="hm-skel hm-skel--line" />
      </div>
    );
  }

  if (status === "error") {
    return (
      <div className="hm-tablecard" role="group" aria-label="Prossima sessione, non disponibile">
        <TableSeats seats={emptySeatLayout(6)} label="Tavolo non disponibile" />
        <h2 className="hm-tablecard__title">Le sessioni non si caricano.</h2>
        <p className="hm-tablecard__meta">Controlla la connessione e riprova.</p>
        <div className="hm-tablecard__foot is-centered">
          <button type="button" className="btn btn-primary" onClick={onRetry}>
            Riprova
          </button>
        </div>
      </div>
    );
  }

  if (!featured) {
    const canOpenFirst = role === "Master" || !role;
    return (
      <div className="hm-tablecard" role="group" aria-label="Nessuna sessione in programma">
        <TableSeats seats={emptySeatLayout(6)} label="Tavolo vuoto, nessuna sessione in programma" animate />
        <h2 className="hm-tablecard__title">Il primo tavolo è ancora vuoto.</h2>
        <p className="hm-tablecard__meta">Quando un Master pubblica una sessione, la trovi qui.</p>
        {canOpenFirst && (
          <div className="hm-tablecard__foot is-centered">
            <button type="button" className="hm-link" onClick={role === "Master" ? onCreateSession : onRegisterMaster}>
              {role === "Master" ? "Crea una sessione" : "Sei un Master? Apri il primo tavolo"}
            </button>
          </div>
        )}
      </div>
    );
  }

  const parts = dateParts(featured.when);
  const tone = seatsTone(featured.seats);
  const meta = [featured.gameTitle, featured.masterName && `con ${featured.masterName}`].filter(Boolean).join(", ");
  const when = parts
    ? `${capitalize(parts.long)} alle ${parts.time}${featured.duration ? `, durata ${featured.duration}` : ""}`
    : "";
  const to = `/sessions/${featured.id}`;
  const canTakeSeat = canJoin(featured) && (!role || role === "Player");
  const cta = isMine ? "Vedi la sessione" : canTakeSeat ? "Prendi posto" : "Vedi i dettagli";
  const flag = isMine ? (role === "Master" ? "La dirigi tu" : "Sei iscritto") : null;

  return (
    <div className="hm-tablecard" role="group" aria-label={isMine ? "La tua prossima sessione" : "Prossima sessione"}>
      {flag && <span className="hm-tablecard__flag">{flag}</span>}
      <TableSeats
        seats={seatLayout(featured)}
        master={{ name: featured.masterName, avatar: featured.masterAvatar }}
        center={parts ? { day: parts.day, month: parts.month, time: parts.time } : null}
        tone={tone}
        label={tableLabel(featured)}
        animate
      />

      <ul className="hm-legend" aria-hidden="true">
        <li>
          <span className="hm-legend__glyph is-master" /> Master
        </li>
        <li>
          <span className="hm-legend__glyph is-taken" /> Iscritto
        </li>
        <li>
          <span className="hm-legend__glyph is-free" /> Libero
        </li>
      </ul>

      <h2 className="hm-tablecard__title">
        <Link to={to}>{featured.title}</Link>
      </h2>
      {meta && <p className="hm-tablecard__meta">{meta}</p>}
      {when && <p className="hm-tablecard__when">{when}</p>}

      <div className="hm-tablecard__foot">
        <span className={`hm-seatstext is-${tone}`}>{seatsLabel(featured.seats)}</span>
        <Link className="btn btn-primary" to={to}>
          {cta}
        </Link>
      </div>
    </div>
  );
}

// ---------------------------------------------------------------------------------------------

export default function HomeHero({
  role,
  name,
  status,
  featured,
  featuredIsMine,
  sessionsCount,
  hostCount,
  mineCount,
  mineLoading,
  pendingCount,
  onRegisterPlayer,
  onRegisterMaster,
  onCreateSession,
  onRetry,
}) {
  const isMember = !!role;

  return (
    <section className="hm-hero" aria-labelledby="hm-hero-title">
      <Sparkles />

      <Container>
        <div className="hm-hero__grid">
          <div className="hm-hero__text">
            {isMember ? (
              <>
                <h1 id="hm-hero-title" className="hm-hero__title">
                  {name ? `Ciao, ${name}.` : "Bentornato."}
                </h1>
                <p className="hm-hero__lead">{memberSentence({ role, mineCount, mineLoading, pendingCount })}</p>

                <div className="hm-actions">
                  {role === "Master" && (
                    <Button variant="primary" size="lg" onClick={onCreateSession}>
                      Crea una sessione
                    </Button>
                  )}
                  {role === "Player" && (
                    <a className="btn btn-primary btn-lg" href="#sessioni" onClick={scrollToSessions}>
                      Cerca un tavolo
                    </a>
                  )}
                  {role === "Admin" && (
                    <Link className="btn btn-primary btn-lg" to="/admin">
                      Apri la dashboard
                    </Link>
                  )}
                  {role !== "Admin" && (
                    <Link className="hm-ghost" to={role === "Master" ? "/masterProfile" : "/playerProfile"}>
                      Il tuo profilo
                    </Link>
                  )}
                  {role !== "Player" && (
                    <a className="hm-ghost" href="#sessioni" onClick={scrollToSessions}>
                      Guarda le sessioni
                    </a>
                  )}
                </div>
              </>
            ) : (
              <>
                <h1 id="hm-hero-title" className="hm-hero__title">
                  Trova il tuo posto al tavolo.
                </h1>
                <p className="hm-hero__lead">
                  Rolayther mette in contatto giocatori e Master di giochi di ruolo. Scegli una sessione, prendi
                  posto e gioca.
                </p>

                <div className="hm-actions">
                  <Button variant="primary" size="lg" onClick={onRegisterPlayer}>
                    Registrati e prendi posto
                  </Button>
                  <a className="hm-ghost" href="#sessioni" onClick={scrollToSessions}>
                    Guarda le sessioni
                  </a>
                </div>

                <p className="hm-hero__master">
                  <button type="button" className="hm-link" onClick={onRegisterMaster}>
                    Sei un Master? Richiedi di dirigere
                  </button>
                </p>

                {sessionsCount > 0 && <p className="hm-hero__pulse">{pulseText(sessionsCount, hostCount)}</p>}
              </>
            )}
          </div>

          <TableCard
            status={status}
            featured={featured}
            isMine={featuredIsMine}
            role={role}
            onRegisterMaster={onRegisterMaster}
            onCreateSession={onCreateSession}
            onRetry={onRetry}
          />
        </div>
      </Container>
    </section>
  );
}
