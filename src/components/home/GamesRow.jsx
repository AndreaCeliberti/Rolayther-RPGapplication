import { Container } from "react-bootstrap";
import { GiDiceTwentyFacesTwenty } from "react-icons/gi";
import SectionHead from "./SectionHead";
import { tintOf } from "../../utils/homeSessions";

// Le locandine dei giochi (con la copertina quando c'è) e, sotto, i generi
export default function GamesRow({ games, genres }) {
  if (games.length === 0 && genres.length === 0) return null;

  return (
    <section className="hm-section hm-games" aria-labelledby="hm-games-title">
      <Container>
        <SectionHead
          id="hm-games-title"
          title="I giochi"
          lead="A cosa si gioca ai tavoli di Rolayther."
          action={{ to: "/games", label: "Tutti i giochi" }}
        />

        {games.length > 0 && (
          <ul className="hm-posters">
            {games.map((g) => (
              <li key={g.id} className="hm-poster">
                <span className={`hm-poster__cover tint-${tintOf(g.title)}`} aria-hidden="true">
                  {g.cover ? <img src={g.cover} alt="" loading="lazy" /> : <GiDiceTwentyFacesTwenty />}
                </span>
                <span className="hm-poster__title">{g.title}</span>
              </li>
            ))}
          </ul>
        )}

        {genres.length > 0 && (
          <ul className="hm-tags" aria-label="Generi">
            {genres.map((g) => (
              <li key={g.id}>{g.name}</li>
            ))}
          </ul>
        )}
      </Container>
    </section>
  );
}
