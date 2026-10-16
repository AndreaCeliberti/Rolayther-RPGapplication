// Estrae un messaggio leggibile da un errore di axios.
// Il backend risponde con { message } / { Message }, oppure con una stringa semplice.
export function apiErrorMessage(err, fallback = "Operazione non riuscita") {
  const data = err?.response?.data;

  if (typeof data === "string" && data.trim()) return data;

  const message = data?.message || data?.Message;
  if (message) return message;

  // Richiesta troppo grande: il server la scarta prima ancora di leggere il corpo
  if (err?.response?.status === 413) return "Il file è troppo pesante.";

  return fallback;
}

// Codice di errore "macchina" che il backend aggiunge ad alcune risposte (es. "EmailNotConfirmed", "TokenExpired"),
// così l'interfaccia può reagire senza confrontare testi.
export function apiErrorCode(err) {
  const data = err?.response?.data;
  return data?.code || data?.Code || null;
}

// Carica un file DOPO il salvataggio principale (creazione o modifica di un elemento).
// Se il caricamento fallisce l'elemento resta comunque salvato: la funzione non lancia mai e
// restituisce "" se è andato tutto bene, oppure il motivo dell'errore da mostrare all'utente.
//
//   uploadFn: (id, file) => Promise   (es. SessionsApi.uploadCover)
export async function uploadAfterSave(file, id, uploadFn) {
  if (!file) return "";
  if (!id) return "il server non ha restituito l'identificativo";

  try {
    await uploadFn(id, file);
    return "";
  } catch (err) {
    return apiErrorMessage(err, "caricamento non riuscito");
  }
}
