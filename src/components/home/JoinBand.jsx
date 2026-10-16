import { Button, Container } from "react-bootstrap";

// Chiusura della pagina per chi non è ancora dentro: due porte, come i due tipi di posto al tavolo
// (il cerchio è il giocatore, il rombo è il Master).
export default function JoinBand({ onRegisterPlayer, onRegisterMaster, onLogin }) {
  return (
    <section className="hm-section hm-join" aria-labelledby="hm-join-title">
      <Container>
        <div className="hm-join__panel">
          <h2 id="hm-join-title" className="hm-join__title">
            Da che parte del tavolo siedi?
          </h2>

          <div className="hm-join__doors">
            <div className="hm-door">
              <span className="hm-door__glyph is-player" aria-hidden="true" />
              <h3 className="hm-door__title">Gioca</h3>
              <p className="hm-door__text">Registrati, conferma l'email e prendi posto alla prima sessione che ti convince.</p>
              <Button variant="primary" onClick={onRegisterPlayer}>
                Registrati come giocatore
              </Button>
            </div>

            <div className="hm-door">
              <span className="hm-door__glyph is-master" aria-hidden="true" />
              <h3 className="hm-door__title">Dirigi</h3>
              <p className="hm-door__text">
                Chiedi di diventare Master: dopo l'approvazione apri i tuoi tavoli e scegli data e posti.
              </p>
              <Button variant="outline-secondary" onClick={onRegisterMaster}>
                Richiedi di diventare Master
              </Button>
            </div>
          </div>

          <p className="hm-join__login">
            Hai già un account?{" "}
            <button type="button" className="hm-link" onClick={onLogin}>
              Accedi
            </button>
          </p>
        </div>
      </Container>
    </section>
  );
}
