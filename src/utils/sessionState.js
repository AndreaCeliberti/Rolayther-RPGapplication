export const SESSION_STATES = {
  Draft: 0,
  Published: 1,
  Full: 2,
  InProgress: 3,
  Completed: 4,
  Cancelled: 5,
};

export const STATE_LABELS = {
  0: "In Arrivo",
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

export function stateBadgeVariant(state) {
  // varianti bootstrap: primary, secondary, success, danger, warning, info, light, dark
  switch (Number(state)) {
    case 0: return "secondary"; // Draft
    case 1: return "primary";   // Published
    case 2: return "warning";   // Full
    case 3: return "info";      // InProgress
    case 4: return "success";   // Completed
    case 5: return "danger";    // Cancelled
    default: return "secondary";
  }
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
