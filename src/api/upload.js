import axios from "axios";
import api from "./axios";
import { apiErrorMessage } from "../utils/apiError";

// Invia un file al backend come multipart/form-data.
// Il campo si chiama "file": è il nome del parametro IFormFile degli endpoint di upload.
// (Nessun Content-Type a mano: lo imposta il browser, con il boundary giusto.)
export function postFile(path, file) {
  const body = new FormData();
  body.append("file", file);
  return api.post(path, body);
}

// Foto profilo scelta durante la registrazione.
//
// Subito dopo la registrazione l'utente non ha ancora una sessione, ma l'upload richiede di essere autenticati.
// Apriamo quindi una sessione "usa e getta" con le credenziali appena create, carichiamo la foto e la chiudiamo:
// l'utente resta disconnesso, esattamente come prima. Nessun token viene salvato nel browser.
//
// Funziona anche per un Master appena registrato (ancora in attesa di approvazione e quindi senza ruolo),
// perché il backend autorizza il caricamento in base all'email del token.
//
// Non lancia mai: restituisce "" se è andato tutto bene, altrimenti il motivo dell'errore.
export async function uploadAvatarAfterRegistration({ role, email, password, file }) {
  if (!file) return "";

  // Client separato, senza gli interceptor di "api": non deve toccare i token dell'app
  const client = axios.create({ baseURL: import.meta.env.VITE_API_URL });

  let auth;
  try {
    const res = await client.post("/Auth/login", { email, password });
    auth = res.data;
    if (!auth?.accessToken) return "accesso non riuscito";
  } catch (err) {
    return apiErrorMessage(err, "accesso non riuscito");
  }

  const headers = { Authorization: `Bearer ${auth.accessToken}` };
  const isMaster = role === "master";

  try {
    const me = await client.get(isMaster ? "/Master/Me" : "/Player/Me", { headers });
    const id = isMaster ? me.data?.masterId : me.data?.playerId;
    if (!id) return "profilo non trovato";

    const body = new FormData();
    body.append("file", file);
    await client.post(isMaster ? `/Master/${id}/Avatar` : `/Player/${id}/Avatar`, body, { headers });
    return "";
  } catch (err) {
    return apiErrorMessage(err, "caricamento non riuscito");
  } finally {
    // Chiude la sessione temporanea (revoca il refresh token)
    try {
      await client.post("/Auth/logout", { refreshToken: auth.refreshToken });
    } catch {
      // non è un problema: il token scade da solo
    }
  }
}
