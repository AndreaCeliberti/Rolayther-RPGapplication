// Informativa sulla privacy: la versione del testo e i dati che solo chi gestisce il sito conosce.
// Il testo vero e proprio sta in src/pages/PrivacyPolicy.jsx. La guida completa è in docs/PRIVACY.md del backend.
//
// DA FARE PRIMA DI PUBBLICARE IL SITO: compila PRIVACY_INFO qui sotto. Finché manca qualcosa, la pagina /privacy
// si presenta come "bozza" e al posto del dato mancante mostra «da completare».
// Il testo è una base di partenza: prima di pubblicarlo conviene farlo rivedere da chi se ne intende.
//
// Quando cambi il testo in modo che conti per chi si iscrive (nuove finalità, nuovi fornitori, nuovi tempi di
// conservazione...), aggiorna PRIVACY_POLICY_VERSION: ogni nuova iscrizione registra quale versione l'utente ha letto.

// Dove si legge l'informativa
export const PRIVACY_PATH = "/privacy";

// Versione del testo, come data AAAA-MM-GG (al massimo 20 caratteri: è la lunghezza della colonna nel database)
export const PRIVACY_POLICY_VERSION = "2026-10-20";

export const PRIVACY_INFO = {
  // Chi decide perché e come si trattano i dati: una persona o una società/associazione.
  // Scrivi il nome ufficiale (per un'associazione, come compare nello statuto).
  // es. "Mario Rossi" oppure "Associazione Rolayther"
  controllerName: "Rolayther associazione ludica",

  // Sede o indirizzo del titolare (facoltativo ma consigliato)
  controllerAddress: "",

  // Casella che leggi davvero: qui arrivano le richieste degli utenti (accesso, correzione, cancellazione...)
  contactEmail: "rolayther@gmail.com",

  // Per quanto tempo si conservano i dati di un account che non si usa più (la frase continua così:
  // "Conserviamo i dati del tuo account finché lo usi e per ...", poi li eliminiamo)
  // Attenzione: l'app non cancella ancora da sola gli account inattivi e non registra l'ultimo accesso.
  // es. "un anno dopo l’ultimo utilizzo" (con l’apostrofo tipografico, come nel resto del testo)
  retention: "un anno dopo l’ultimo utilizzo",

  // Servizi esterni che trattano dati per conto di Rolayther: hosting, invio delle email, archiviazione delle immagini
  // es. [{ name: "Brevo", purpose: "invio delle email" }]
  providers: [],

  // Età minima per iscriversi (numero). In Italia da 14 anni si può dare il consenso da soli.
  // Attenzione: il sito non controlla l'età, la dichiara soltanto.
  minAge: 14,
};

// Elenco di ciò che manca: la pagina è una "bozza" finché non è vuoto
export function missingPrivacyInfo(info = PRIVACY_INFO) {
  const missing = [];
  if (!info.controllerName) missing.push("titolare del trattamento");
  if (!info.contactEmail) missing.push("email di contatto");
  if (!info.retention) missing.push("tempi di conservazione");
  if (!info.providers || info.providers.length === 0) missing.push("fornitori");
  if (!info.minAge) missing.push("età minima");
  return missing;
}
