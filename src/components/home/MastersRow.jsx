import { Link } from "react-router-dom";
import { Container } from "react-bootstrap";
import SectionHead from "./SectionHead";
import { initialOf, pluralize, tintOf } from "../../utils/homeSessions";

// I Master con più sessioni in programma, ognuno con il collegamento al suo profilo
export default function MastersRow({ masters }) {
  if (masters.length === 0) return null;

  return (
    <section className="hm-section hm-masters" aria-labelledby="hm-masters-title">
      <Container>
        <SectionHead
          id="hm-masters-title"
          title="I Master"
          lead="Chi dirige i tavoli: apri un profilo per vedere le sue sessioni."
          action={{ to: "/masters", label: "Tutti i Master" }}
        />

        <ul className="hm-people">
          {masters.map((m) => (
            <li key={m.id}>
              <Link className="hm-person" to={`/masters/${m.id}`}>
                <span className={`hm-person__avatar tint-${tintOf(m.name)}`} aria-hidden="true">
                  {m.avatar ? <img src={m.avatar} alt="" loading="lazy" /> : <span>{initialOf(m.name)}</span>}
                </span>
                <span className="hm-person__name">{m.name}</span>
                {m.upcoming > 0 && (
                  <span className="hm-person__meta">
                    {m.upcoming} {pluralize(m.upcoming, "sessione", "sessioni")} in programma
                  </span>
                )}
              </Link>
            </li>
          ))}
        </ul>
      </Container>
    </section>
  );
}
