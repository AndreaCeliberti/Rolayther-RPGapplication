import { Link } from "react-router-dom";
import { Container } from "react-bootstrap";
import { GiDiceTwentyFacesTwenty } from "react-icons/gi";
import SectionHead from "./SectionHead";
import TableSeats from "./TableSeats";
import { dateParts, seatLayout, seatsLabel, seatsTone, STATE, tintOf } from "../../utils/homeSessions";

const capitalize = (text) => text.charAt(0).toUpperCase() + text.slice(1);

// Una riga dell'agenda: la data a sinistra, il titolo al centro, il tavolo con i posti a destra.
// Tutta la riga è un collegamento alla pagina della sessione.
function Row({ session }) {
  const parts = dateParts(session.when);
  const tone = seatsTone(session.seats);
  const meta = [session.gameTitle, session.masterName && `con ${session.masterName}`].filter(Boolean).join(", ");
  const when = parts ? `${parts.time}${session.duration ? `, durata ${session.duration}` : ""}` : "";

  return (
    <li className="hm-agenda__item">
      <Link className="hm-row" to={`/sessions/${session.id}`}>
        <span className="hm-row__date" aria-hidden="true">
          {parts ? (
            <>
              <span className="hm-row__weekday">{parts.weekday}</span>
              <span className="hm-row__day">{parts.day}</span>
              <span className="hm-row__month">{parts.month}</span>
            </>
          ) : (
            <span className="hm-row__weekday">Data da fissare</span>
          )}
        </span>

        <span className={`hm-row__cover tint-${tintOf(session.title)}`} aria-hidden="true">
          {session.cover ? <img src={session.cover} alt="" loading="lazy" /> : <GiDiceTwentyFacesTwenty />}
        </span>

        <span className="hm-row__main">
          <span className="hm-row__title">{session.title}</span>
          {meta && <span className="hm-row__meta">{meta}</span>}
          {parts && (
            <span className="hm-row__when">
              <span className="visually-hidden">{capitalize(parts.long)}, </span>
              {when}
            </span>
          )}
          {session.state === STATE.InProgress && <span className="hm-row__flag">In corso</span>}
          {session.state === STATE.Draft && <span className="hm-row__flag">In arrivo</span>}
        </span>

        <span className="hm-row__seats">
          <TableSeats size="sm" seats={seatLayout(session)} tone={tone} />
          <span className={`hm-seatstext is-${tone}`}>{seatsLabel(session.seats)}</span>
        </span>
      </Link>
    </li>
  );
}

function SkeletonRows({ count = 3 }) {
  return Array.from({ length: count }, (_, i) => (
    <li key={i} className="hm-agenda__item" aria-hidden="true">
      <div className="hm-row hm-row--skeleton">
        <span className="hm-row__date">
          <span className="hm-skel hm-skel--date" />
        </span>
        <span className="hm-row__cover">
          <span className="hm-skel hm-skel--cover" />
        </span>
        <span className="hm-row__main">
          <span className="hm-skel hm-skel--title" />
          <span className="hm-skel hm-skel--line" />
        </span>
        <span className="hm-row__seats">
          <span className="hm-skel hm-skel--mini" />
        </span>
      </div>
    </li>
  ));
}

// status: "loading" | "ready" | "error"
// empty: cosa mostrare quando non c'è nessuna sessione (un testo e, se serve, un'azione)
export default function SessionAgenda({
  id,
  headingId,
  title,
  lead,
  status,
  sessions,
  limit = 6,
  action,
  empty,
  waking = false,
  onRetry,
}) {
  const shown = sessions.slice(0, limit);

  return (
    <section id={id} tabIndex={id ? -1 : undefined} className="hm-section hm-agenda" aria-labelledby={headingId}>
      <Container>
        <div className="hm-agenda__layout">
          <SectionHead id={headingId} title={title} lead={lead} action={action} />

          <div className="hm-agenda__body" aria-live="polite" aria-busy={status === "loading"}>
            {status === "loading" && (
              <>
                {waking && (
                  <p className="hm-note" role="status">
                    Il server si sta svegliando: la prima volta ci vuole circa un minuto.
                  </p>
                )}
                <ul className="hm-agenda__list">
                  <SkeletonRows />
                </ul>
              </>
            )}

            {status === "error" && (
              <div className="hm-empty" role="alert">
                <p className="hm-empty__title">Non riesco a caricare le sessioni.</p>
                <p className="hm-empty__text">Controlla la connessione e riprova.</p>
                <button type="button" className="btn btn-primary" onClick={onRetry}>
                  Riprova
                </button>
              </div>
            )}

            {status === "ready" && shown.length === 0 && (
              <div className="hm-empty">
                <p className="hm-empty__title">{empty?.title}</p>
                {empty?.text && <p className="hm-empty__text">{empty.text}</p>}
                {empty?.action && (
                  <button type="button" className="btn btn-primary" onClick={empty.action.onClick}>
                    {empty.action.label}
                  </button>
                )}
              </div>
            )}

            {status === "ready" && shown.length > 0 && (
              <ul className="hm-agenda__list">
                {shown.map((s) => (
                  <Row key={s.id} session={s} />
                ))}
              </ul>
            )}
          </div>
        </div>
      </Container>
    </section>
  );
}
