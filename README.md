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
