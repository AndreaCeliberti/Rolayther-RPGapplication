import { useEffect } from "react";
import { Card, Container } from "react-bootstrap";
import PageHeader from "../components/common/PageHeader";
import { PRIVACY_INFO, PRIVACY_POLICY_VERSION, missingPrivacyInfo } from "../config/privacy";
import "../styles/privacy.css";

// Informativa sulla privacy. È una base di partenza scritta sui dati che l'app raccoglie davvero:
// prima di pubblicare il sito va completata (src/config/privacy.js) e fatta rivedere da chi se ne intende.
// Se cambi il testo in modo che conti per chi si iscrive, aggiorna PRIVACY_POLICY_VERSION in quel file.

// "2026-10-20" -> "20 ottobre 2026"
function versionDate(version) {
  const d = new Date(`${version}T00:00:00Z`);
  if (Number.isNaN(d.getTime())) return version;
  return d.toLocaleDateString("it-IT", { day: "numeric", month: "long", year: "numeric", timeZone: "UTC" });
}

// Un dato che manca si vede come «da completare», nel punto esatto in cui va inserito
function Todo({ children }) {
  return children ? <>{children}</> : <mark className="privacy-todo">da completare</mark>;
}

function Section({ id, title, children }) {
  return (
    <section id={id} aria-labelledby={`${id}-titolo`}>
      <h2 id={`${id}-titolo`}>{title}</h2>
      {children}
    </section>
  );
}

export default function PrivacyPolicy() {
  const info = PRIVACY_INFO;
  const draft = missingPrivacyInfo(info).length > 0;

  // Aprendo la pagina dal piè di pagina il browser resterebbe a metà: si riparte dall'inizio
  useEffect(() => {
    window.scrollTo({ top: 0, left: 0, behavior: "instant" });
  }, []);

  const email = info.contactEmail ? <a href={`mailto:${info.contactEmail}`}>{info.contactEmail}</a> : null;
  const providers = (info.providers || [])
    .map((p) => (p.purpose ? `${p.name} (${p.purpose})` : p.name))
    .join(", ");

  return (
    <>
      <PageHeader
        title="Informativa sulla privacy"
        subtitle={`Come Rolayther tratta i tuoi dati personali. Versione del ${versionDate(PRIVACY_POLICY_VERSION)}.`}
      />

      <Container fluid="md" className="px-3 px-md-0 pb-4">
        <div className="privacy-page mx-auto">
          {draft && (
            <div className="privacy-draft mb-3" role="note">
              <strong>Bozza.</strong> Questo documento è in revisione: le informazioni segnate come{" "}
              <mark className="privacy-todo">da completare</mark> verranno inserite prima della pubblicazione del sito.
            </div>
          )}

          <Card className="privacy-doc border-0 shadow-sm">
            <Card.Body>
              <section className="privacy-summary" aria-labelledby="in-breve-titolo">
                <h2 id="in-breve-titolo">In breve</h2>
                <ul>
                  <li>Chiediamo solo i dati che servono per creare il tuo profilo e farti partecipare alle sessioni.</li>
                  <li>
                    Il tuo nickname e la tua foto sono visibili a tutti, per esempio nelle liste dei partecipanti. Nome,
                    cognome, email e data di nascita no.
                  </li>
                  <li>Non vendiamo i tuoi dati e non li usiamo per fare pubblicità.</li>
                  <li>
                    Puoi chiedere di vederli, correggerli o cancellarli scrivendo a <Todo>{email}</Todo>.
                  </li>
                </ul>
              </section>

              <Section id="titolare" title="Chi tratta i tuoi dati">
                <p>
                  Il titolare del trattamento, cioè chi decide perché e come si usano i tuoi dati, è{" "}
                  <Todo>{info.controllerName}</Todo>
                  {info.controllerAddress ? `, ${info.controllerAddress}` : ""}.
                </p>
                <p>
                  Per qualsiasi domanda sui tuoi dati o per esercitare i tuoi diritti scrivi a <Todo>{email}</Todo>.
                </p>
              </Section>

              <Section id="dati" title="Quali dati raccogliamo">
                <ul>
                  <li>
                    <strong>Quando ti registri:</strong> nome, cognome, nickname, data di nascita, indirizzo email e
                    password. La password la conserviamo solo in una forma che non permette di risalire alla password
                    vera, quindi nessuno può leggerla.
                  </li>
                  <li>
                    <strong>Se vuoi:</strong> una foto profilo e una breve bio.
                  </li>
                  <li>
                    <strong>Mentre usi il sito:</strong> le sessioni a cui ti iscrivi o che organizzi e le immagini
                    che carichi, come la copertina di una sessione.
                  </li>
                  <li>
                    <strong>Il tuo consenso:</strong> la data in cui l’hai dato e la versione di questa informativa che
                    avevi davanti, come prova che il consenso c’è stato.
                  </li>
                  <li>
                    <strong>Dati tecnici:</strong> per tenerti collegato, il tuo browser conserva alcune informazioni di
                    accesso. Il server può registrare in log tecnici le richieste ricevute e gli errori, e usa
                    l’indirizzo IP per limitare le richieste ripetute, per esempio i tentativi di accesso.
                  </li>
                  <li>
                    <strong>Email di servizio:</strong> ti scriviamo solo per il funzionamento del sito, cioè per
                    confermare il tuo indirizzo, comunicarti l’esito di una richiesta Master e confermarti iscrizioni e
                    nuove sessioni. Non mandiamo comunicazioni pubblicitarie.
                  </li>
                </ul>
                <p>Non usiamo cookie di profilazione né strumenti di pubblicità o di analisi del comportamento.</p>
              </Section>

              <Section id="finalita" title="Perché li usiamo e su quale base">
                <ul>
                  <li>
                    <strong>Creare e gestire il tuo account e farti partecipare alle sessioni</strong>, comprese le
                    email di servizio. Ci basiamo sul tuo consenso, che dai spuntando la casella quando ti iscrivi
                    (art. 6, par. 1, lett. a del GDPR), e sulla necessità di fornirti il servizio che hai richiesto
                    (lett. b).
                  </li>
                  <li>
                    <strong>Tenere il sito sicuro e prevenire gli abusi.</strong> Ci basiamo sul nostro legittimo
                    interesse (lett. f).
                  </li>
                  <li>
                    <strong>Rispettare obblighi di legge</strong>, per esempio rispondere a richieste delle autorità
                    (lett. c).
                  </li>
                </ul>
                <p>
                  Per iscriverti devi accettare questo trattamento, perché senza questi dati l’account non può esistere.
                  Puoi ritirare il consenso quando vuoi: in quel caso chiudiamo l’account.
                </p>
              </Section>

              <Section id="destinatari" title="Chi può vedere i tuoi dati">
                <ul>
                  <li>
                    <strong>Chiunque visiti il sito</strong>, anche senza account, vede il tuo nickname e la tua foto,
                    per esempio nelle liste dei partecipanti; se sei Master, anche la tua bio e le tue sessioni. Non
                    vede mai nome, cognome, email o data di nascita.
                  </li>
                  <li>
                    <strong>Gli amministratori del sito</strong> vedono i dati del tuo account, perché devono gestirlo
                    e valutare le richieste per diventare Master.
                  </li>
                  <li>
                    <strong>I fornitori che fanno funzionare il servizio</strong> (hosting, invio delle email,
                    archiviazione delle immagini) trattano i dati per nostro conto e solo per questo scopo:{" "}
                    <Todo>{providers}</Todo>. Se un fornitore ha sede fuori dallo Spazio economico europeo, il
                    trasferimento avviene con le garanzie previste dal GDPR.
                  </li>
                  <li>
                    <strong>Google Fonts.</strong> Il carattere del sito (Inter) viene scaricato dai server di Google:
                    per farlo, il tuo browser contatta Google, che riceve il tuo indirizzo IP.
                  </li>
                </ul>
                <p>
                  Le immagini che carichi, come la foto profilo e le copertine, sono pubbliche: chi conosce il loro
                  indirizzo può vederle. Non vendiamo i tuoi dati e non li cediamo ad altri per scopi pubblicitari.
                </p>
              </Section>

              <Section id="conservazione" title="Per quanto tempo li conserviamo">
                <p>
                  Conserviamo i dati del tuo account finché l’account resta attivo. Se ne chiedi la cancellazione, li
                  eliminiamo <Todo>{info.retentionAfterDeletion}</Todo>, tranne quelli che la legge ci obbliga a
                  conservare. I log tecnici restano solo il tempo necessario a garantire la sicurezza del sito.
                </p>
              </Section>

              <Section id="diritti" title="I tuoi diritti">
                <p>Puoi in qualsiasi momento:</p>
                <ul>
                  <li>sapere quali dati abbiamo su di te e riceverne una copia (accesso e portabilità);</li>
                  <li>far correggere i dati sbagliati o incompleti (rettifica);</li>
                  <li>chiedere di cancellare i tuoi dati o di limitarne l’uso;</li>
                  <li>opporti ai trattamenti basati sul nostro legittimo interesse;</li>
                  <li>ritirare il consenso, senza che questo cambi ciò che è stato fatto prima.</li>
                </ul>
                <p>
                  Per esercitare questi diritti scrivi a <Todo>{email}</Todo>: rispondiamo entro un mese. Se pensi che i
                  tuoi dati non siano trattati correttamente puoi anche fare reclamo al{" "}
                  <a href="https://www.garanteprivacy.it" target="_blank" rel="noopener noreferrer">
                    Garante per la protezione dei dati personali
                  </a>
                  .
                </p>
              </Section>

              <Section id="eta" title="Chi può iscriversi">
                <p>
                  Per iscriverti devi avere almeno <Todo>{info.minAge ? `${info.minAge} anni` : null}</Todo>.
                </p>
              </Section>

              <Section id="modifiche" title="Se questo documento cambia">
                <p>
                  Quando cambiamo l’informativa aggiorniamo la data e la versione in cima alla pagina. Ogni volta che
                  qualcuno si iscrive registriamo quale versione ha letto.
                </p>
              </Section>
            </Card.Body>
          </Card>
        </div>
      </Container>
    </>
  );
}
