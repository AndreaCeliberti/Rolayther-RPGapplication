import { useCallback, useEffect, useRef, useState } from "react";
import { SessionsApi } from "../api/sessions.api";
import { MastersApi } from "../api/masters.api";
import { GamesApi } from "../api/games.api";
import { GenresApi } from "../api/genres.api";

const LOADING = { status: "loading", items: [] };
const INITIAL = { sessions: LOADING, masters: LOADING, games: LOADING, genres: LOADING, now: 0 };

// Dopo quanto la Home avvisa che il server si sta svegliando (sul piano gratuito di Render si spegne quando nessuno lo usa)
const WAKING_AFTER_MS = 5000;

// Il backend risponde 404 a una lista vuota: per la Home vuol dire "nessun elemento", non "errore"
async function loadList(request) {
  try {
    const res = await request();
    return { status: "ready", items: Array.isArray(res.data) ? res.data : [] };
  } catch (err) {
    if (err?.response?.status === 404) return { status: "ready", items: [] };
    return { status: "error", items: [] };
  }
}

// Carica in parallelo sessioni, Master, giochi e generi. Ogni lista compare appena arriva:
// se una fallisce, le altre si vedono comunque.
export function useHomeData() {
  const [data, setData] = useState(INITIAL);
  const [slow, setSlow] = useState(false);
  const runRef = useRef(0);

  const start = useCallback(() => {
    const id = ++runRef.current;
    const isCurrent = () => id === runRef.current;

    const put = (key) => (result) => {
      if (!isCurrent()) return; // risposta di una richiesta ormai superata
      setData((d) => ({ ...d, [key]: result, ...(key === "sessions" ? { now: Date.now() } : {}) }));
    };

    const timer = setTimeout(() => {
      if (isCurrent()) setSlow(true);
    }, WAKING_AFTER_MS);

    Promise.all([
      loadList(() => SessionsApi.getAll()).then(put("sessions")),
      loadList(() => MastersApi.getAll()).then(put("masters")),
      loadList(() => GamesApi.getAll()).then(put("games")),
      loadList(() => GenresApi.getAll()).then(put("genres")),
    ]).then(() => {
      clearTimeout(timer);
      if (isCurrent()) setSlow(false);
    });

    return () => {
      runRef.current += 1;
      clearTimeout(timer);
    };
  }, []);

  useEffect(() => start(), [start]);

  const retry = useCallback(() => {
    setData(INITIAL);
    setSlow(false);
    start();
  }, [start]);

  return { data, retry, waking: slow && data.sessions.status === "loading" };
}
