export const SESSION_STATES = {
  Draft: 0,
  Published: 1,
  Full: 2,
  InProgress: 3,
  Completed: 4,
  Cancelled: 5,
};

export const STATE_LABELS = {
  0: "In arrivo",
  1: "Pubblicata",
  2: "Piena",
  3: "In corso",
  4: "Completata",
  5: "Annullata",
};

export function stateLabel(state) {
  if (state == null) return "—";
  return STATE_LABELS[state] ?? String(state);
}

// Sfogliando l'elenco delle sessioni conta solo ciò che cambia qualcosa: "Pubblicata" è la normalità
// e "Piena" è già detta dal badge dei posti ("Al completo"), quindi non si ripetono su ogni card.
const QUIET_STATES = [SESSION_STATES.Published, SESSION_STATES.Full];

export function isNoteworthyState(state) {
  if (state == null) return false;
  return !QUIET_STATES.includes(Number(state));
}

export const ALLOWED_TRANSITIONS = {
  0: [1, 2, 3,4, 5],     // Draft -> Published, Cancelled
  1: [0, 2, 3, 4, 5],     // Published -> Full, Cancelled
  2: [0, 1, 3, 4, 5],     // Full -> InProgress, Cancelled
  3: [0, 1, 2, 4, 5],        // InProgress -> Completed
  4: [],         // Completed -> none
  5: [],         // Cancelled -> none
};

export function allowedNextStates(currentState) {
  const s = Number(currentState ?? 0);
  return ALLOWED_TRANSITIONS[s] ?? [];
}
