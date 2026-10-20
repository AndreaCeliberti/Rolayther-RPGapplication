# React + Vite

This template provides a minimal setup to get React working in Vite with HMR and some ESLint rules.

Currently, two official plugins are available:

- [@vitejs/plugin-react](https://github.com/vitejs/vite-plugin-react/blob/main/packages/plugin-react) uses [Babel](https://babeljs.io/) (or [oxc](https://oxc.rs) when used in [rolldown-vite](https://vite.dev/guide/rolldown)) for Fast Refresh
- [@vitejs/plugin-react-swc](https://github.com/vitejs/vite-plugin-react/blob/main/packages/plugin-react-swc) uses [SWC](https://swc.rs/) for Fast Refresh

## React Compiler

The React Compiler is not enabled on this template because of its impact on dev & build performances. To add it, see [this documentation](https://react.dev/learn/react-compiler/installation).

## Expanding the ESLint configuration

If you are developing a production application, we recommend using TypeScript with type-aware lint rules enabled. Check out the [TS template](https://github.com/vitejs/vite/tree/main/packages/create-vite/template-react-ts) for information on how to integrate TypeScript and [`typescript-eslint`](https://typescript-eslint.io) in your project.

## Email

Le email di Rolayther (conferma registrazione, approvazione Master, iscrizioni, nuove sessioni) le spedisce il backend: la guida completa è in `docs/EMAIL.md` del repository backend. Lato frontend:

- `public/email/` contiene le immagini usate nelle email (testata, emblema, icone). Il backend le legge da `<indirizzo del sito>/email/...`: non rinominarle né spostarle, e pubblicale insieme al sito.
- `/confirm-email` (`src/pages/ConfirmEmailPage.jsx`) è la pagina aperta dal pulsante «Conferma la mia email».
- `/?login=1` apre la finestra di accesso: è il link del pulsante «Accedi a Rolayther» delle email.

## Privacy

Chi si registra (Player o richiesta Master) deve spuntare la casella «Ho letto l'informativa sulla privacy e acconsento al trattamento dei miei dati personali» e può leggere il documento alla pagina `/privacy`, raggiungibile anche dal piè di pagina. Il backend registra data e versione del consenso: la guida completa è in `docs/PRIVACY.md` del repository backend. Lato frontend:

- `src/config/privacy.js` contiene la versione dell'informativa e i dati che solo il titolare conosce (nome, email di contatto, tempi di conservazione, fornitori, età minima). **Vanno completati prima di pubblicare il sito**: finché mancano, `/privacy` si presenta come bozza. Quando cambi il testo in modo che conti per chi si iscrive, aggiorna `PRIVACY_POLICY_VERSION`.
- `src/pages/PrivacyPolicy.jsx` è il testo dell'informativa (una base di partenza, da far rivedere prima di pubblicarla) e `src/components/auth/PrivacyConsentCheck.jsx` è la casella dei moduli di registrazione.
- Le finestre con cui l'Admin crea un account per conto di qualcun altro non chiedono il consenso: può darlo solo l'interessato.
- **Cosa l'informativa promette e l'app non fa ancora:** eliminare i dati un anno dopo l'ultimo utilizzo (l'app non registra l'ultimo accesso e non cancella da sola gli account inattivi) e iscrivere solo chi ha almeno 14 anni (il sito non controlla la data di nascita). Finché non vengono aggiunti, vanno rispettati a mano.
