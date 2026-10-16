// Funzioni pure per la Home: dalle sessioni grezze del server ricavano ciò che serve a mostrarle
// (data, posti, "è ancora in programma?"). Il server manda due forme della stessa sessione
// (pubblica, e completa per l'Admin): qui si leggono entrambe.

export const STATE = { Draft: 0, Published: 1, Full: 2, InProgress: 3, Completed: 4, Cancelled: 5 };

// Una sessione iniziata da meno di 3 ore resta tra quelle "in programma"
const GRACE_MS = 3 * 60 * 60 * 1000;

// Quanti posti si disegnano al massimo attorno al tavolo (oltre, a dirlo è il testo)
export const MAX_SEATS_DRAWN = 12;

// ---------------------------------------------------------------------------------------------
// Testi

export function pluralize(n, one, many) {
  return n === 1 ? one : many;
}

const DAY = new Intl.DateTimeFormat("it-IT", { day: "numeric" });
const MONTH = new Intl.DateTimeFormat("it-IT", { month: "short" });
const WEEKDAY = new Intl.DateTimeFormat("it-IT", { weekday: "short" });
const TIME = new Intl.DateTimeFormat("it-IT", { hour: "2-digit", minute: "2-digit", hour12: false });
const LONG = new Intl.DateTimeFormat("it-IT", { weekday: "long", day: "numeric", month: "long" });

const noDots = (text) => text.replace(/\./g, "");

// Pezzi di data già pronti da mostrare: giorno, mese breve, giorno della settimana, ora
export function dateParts(date) {
  if (!date) return null;
  return {
    day: DAY.format(date),
    month: noDots(MONTH.format(date)),
    weekday: noDots(WEEKDAY.format(date)),
    time: TIME.format(date),
    long: LONG.format(date),
  };
}

export function initialOf(text) {
  const first = String(text || "").trim().charAt(0);
  return first ? first.toLocaleUpperCase("it-IT") : "?";
}

// Sempre lo stesso colore di riserva per lo stesso titolo (0..3), quando manca l'immagine
export function tintOf(text) {
  let hash = 0;
  for (const ch of String(text || "")) hash = (hash * 31 + ch.codePointAt(0)) >>> 0;
  return hash % 4;
}

// Il server manda lo stato come numero; per sicurezza si capisce anche il nome ("Published")
function stateOf(value) {
  if (value == null) return STATE.Published;
  if (typeof value === "string" && Number.isNaN(Number(value))) {
    const key = Object.keys(STATE).find((k) => k.toLowerCase() === value.toLowerCase());
    return key ? STATE[key] : STATE.Published;
  }
  return Number(value);
}

function sameId(a, b) {
  return a != null && b != null && String(a).toLowerCase() === String(b).toLowerCase();
}

// ---------------------------------------------------------------------------------------------
// Sessioni

function toDate(value) {
  if (!value) return null;
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? null : date;
}

export function normalizeSession(raw) {
  const players = Array.isArray(raw.players) ? raw.players : [];
  const max = Number(raw.numbOfPlayer) > 0 ? Number(raw.numbOfPlayer) : 0;
  const taken = players.length;
  const unlimited = max === 0;
  const free = unlimited ? null : Math.max(0, max - taken);
  const state = stateOf(raw.currentState);

  return {
    id: raw.sessionId,
    title: raw.sessionTitle || "Sessione senza titolo",
    when: toDate(raw.scheduledAt),
    duration: raw.duration || "",
    cover: raw.coverImgUrl || null,
    gameTitle: raw.game?.title || "",
    masterId: raw.masterId ?? raw.master?.masterId ?? null,
    masterName: raw.master?.nickName || "",
    masterAvatar: raw.master?.avatarImgUrl || null,
    state,
    players: players.map((p) => ({ id: p.playerId, name: p.nickName || "", avatar: p.avatarImgUrl || null })),
    seats: { max, taken, free, unlimited, isFull: state === STATE.Full || (!unlimited && free === 0) },
  };
}

// "In programma" = non conclusa né annullata e non iniziata da troppo tempo
export function isUpcoming({ state, when }, nowMs) {
  if (state === STATE.Completed || state === STATE.Cancelled) return false;
  if (state === STATE.InProgress) return true;
  if (!when) return true;
  return when.getTime() >= nowMs - GRACE_MS;
}

export function upcomingSessions(rawList, nowMs) {
  return rawList
    .map(normalizeSession)
    .filter((s) => isUpcoming(s, nowMs))
    .sort((a, b) => (a.when?.getTime() ?? Infinity) - (b.when?.getTime() ?? Infinity));
}

// Ci si può iscrivere solo a una sessione pubblicata e con posti liberi (la stessa regola della pagina della sessione)
export function canJoin(session) {
  return session.state === STATE.Published && !session.seats.isFull;
}

// La sessione da mettere in primo piano: la prima a cui ci si può iscrivere; se non ce n'è, la prima con posti liberi;
// se sono tutte piene, la prima
export function pickFeatured(list) {
  return list.find(canJoin) ?? list.find((s) => !s.seats.isFull) ?? list[0] ?? null;
}

// ---------------------------------------------------------------------------------------------
// Posti al tavolo

// Un elemento per ogni posto da disegnare: occupato (con nome e foto) oppure libero
export function seatLayout(session) {
  const { seats, players } = session;
  const total = seats.unlimited ? seats.taken + 1 : Math.max(seats.max, seats.taken);
  const drawn = Math.max(1, Math.min(total, MAX_SEATS_DRAWN));

  return Array.from({ length: drawn }, (_, i) =>
    i < seats.taken && players[i]
      ? { filled: true, name: players[i].name, avatar: players[i].avatar }
      : { filled: false, name: "", avatar: null }
  );
}

// Tavolo vuoto, per quando non c'è ancora nessuna sessione
export function emptySeatLayout(count = 6) {
  return Array.from({ length: count }, () => ({ filled: false, name: "", avatar: null }));
}

export function seatsLabel(seats) {
  if (seats.unlimited) {
    if (seats.taken === 0) return "Posti aperti";
    return `${seats.taken} ${pluralize(seats.taken, "iscritto", "iscritti")}`;
  }
  if (seats.isFull) return "Al completo";
  if (seats.free === 1) return "Ultimo posto libero";
  return `${seats.free} posti liberi`;
}

// full = tutto occupato, last = resta un solo posto, open = c'è ancora spazio
export function seatsTone(seats) {
  if (seats.isFull) return "full";
  if (!seats.unlimited && seats.free === 1) return "last";
  return "open";
}

export function tableLabel(session) {
  const { seats } = session;
  if (seats.unlimited) return `Tavolo con ${seats.taken} ${pluralize(seats.taken, "iscritto", "iscritti")}, posti aperti`;
  return `Tavolo da ${seats.max} posti: ${seats.taken} ${pluralize(seats.taken, "occupato", "occupati")}, ${seats.free} ${pluralize(seats.free, "libero", "liberi")}`;
}

// ---------------------------------------------------------------------------------------------
// Master, giochi, generi

export function featuredMasters(rawMasters, nowMs, limit = 6) {
  return rawMasters
    .map((m) => {
      const sessions = Array.isArray(m.sessions) ? m.sessions : [];
      const upcoming = sessions.filter((s) =>
        isUpcoming({ state: stateOf(s.currentState), when: toDate(s.scheduledAt) }, nowMs)
      ).length;
      return { id: m.masterId, name: m.nickName || "Master", avatar: m.avatarImgUrl || null, upcoming, total: sessions.length };
    })
    .sort((a, b) => b.upcoming - a.upcoming || b.total - a.total || a.name.localeCompare(b.name, "it"))
    .slice(0, limit);
}

export function featuredGames(rawGames, limit = 6) {
  const games = rawGames.map((g) => ({ id: g.gameId, title: g.title || "Gioco", cover: g.coverImageUrl || null }));
  // chi ha la copertina va avanti: la fila è fatta di locandine
  return [...games.filter((g) => g.cover), ...games.filter((g) => !g.cover)].slice(0, limit);
}

export function featuredGenres(rawGenres, limit = 10) {
  return rawGenres.slice(0, limit).map((g) => ({ id: g.genreId, name: g.name || "Genere" }));
}

// ---------------------------------------------------------------------------------------------
// Tutto ciò che la Home mostra, calcolato dai dati ricevuti e dall'utente collegato (se c'è)

export function buildHomeView(data, user, member) {
  const upcoming = data.sessions.status === "ready" ? upcomingSessions(data.sessions.items, data.now) : [];

  let mine = [];
  if (user?.role === "Player" && user.playerId) {
    mine = upcoming.filter((s) => s.players.some((p) => sameId(p.id, user.playerId)));
  } else if (user?.role === "Master" && member.masterId) {
    mine = upcoming.filter((s) => sameId(s.masterId, member.masterId));
  }

  const mineIds = new Set(mine.map((s) => s.id));

  return {
    upcoming,
    mine,
    openTables: upcoming.filter((s) => canJoin(s) && !mineIds.has(s.id)),
    featured: mine[0] ?? pickFeatured(upcoming),
    featuredIsMine: mine.length > 0,
    // quanti Master dirigono le sessioni in programma (e non quanti ne esistono)
    hostCount: new Set(upcoming.map((s) => s.masterId).filter(Boolean)).size,
    masters: data.masters.status === "ready" ? featuredMasters(data.masters.items, data.now) : [],
    games: data.games.status === "ready" ? featuredGames(data.games.items) : [],
    genres: data.genres.status === "ready" ? featuredGenres(data.genres.items) : [],
  };
}
