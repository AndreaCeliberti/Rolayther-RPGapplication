import { Container } from "react-bootstrap";
import SectionHead from "./SectionHead";

// I tre passi sono una vera sequenza (scegli, iscriviti, gioca): per questo qui i numeri hanno senso
const STEPS = [
  {
    title: "Scegli un tavolo",
    text: "Ogni sessione mostra data, gioco, Master e quanti posti restano ancora liberi.",
  },
  {
    title: "Prendi posto",
    text: "Registrati, conferma l'email e iscriviti alla sessione: ti arriva subito la conferma.",
  },
  {
    title: "Gioca",
    text: "Nei dettagli della sessione trovi il link per collegarti all'orario stabilito.",
  },
];

export default function HowItWorks() {
  return (
    <section className="hm-section hm-how" aria-labelledby="hm-how-title">
      <Container>
        <SectionHead id="hm-how-title" title="Tre passi per sederti" />
        <ol className="hm-steps">
          {STEPS.map((step, i) => (
            <li key={step.title} className="hm-step">
              <span className="hm-step__num" aria-hidden="true">
                {i + 1}
              </span>
              <h3 className="hm-step__title">{step.title}</h3>
              <p className="hm-step__text">{step.text}</p>
            </li>
          ))}
        </ol>
      </Container>
    </section>
  );
}
