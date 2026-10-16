import { useEffect, useState } from "react";
import { PlayersApi } from "../api/players.api";
import { MastersApi } from "../api/masters.api";

const NONE = { ready: false, nickName: "", masterId: null, pendingCount: null };

// Ciò che alla Home serve sapere dell'utente collegato oltre al ruolo:
// il soprannome (Player e Master), l'id del Master (per trovare le sue sessioni) e quante richieste Master aspettano (Admin).
// `ready` diventa vero quando la risposta è arrivata (o è fallita): finché non lo è, la Home non tira conclusioni.
// Se il server non risponde si va avanti senza soprannome.
export function useMemberInfo(user) {
  const role = user?.role ?? null;
  const email = user?.email ?? null;
  const key = role ? `${role}:${email}` : null;

  const [info, setInfo] = useState({ key: null, ...NONE });

  useEffect(() => {
    if (!key) return undefined;

    let cancelled = false;
    const done = (patch) => {
      if (!cancelled) setInfo({ key, ...NONE, ready: true, ...patch });
    };

    if (role === "Player") {
      PlayersApi.getMe()
        .then((res) => done({ nickName: res.data?.nickName || "" }))
        .catch(() => done({}));
    } else if (role === "Master") {
      MastersApi.getMe()
        .then((res) => done({ nickName: res.data?.nickName || "", masterId: res.data?.masterId || null }))
        .catch(() => done({}));
    } else if (role === "Admin") {
      MastersApi.getPending()
        .then((res) => done({ pendingCount: Array.isArray(res.data) ? res.data.length : 0 }))
        .catch(() => done({}));
    }

    return () => {
      cancelled = true;
    };
  }, [key, role]);

  // Dati di un altro utente (ci si è scollegati e ricollegati): non si usano
  return info.key === key ? info : { key, ...NONE };
}
