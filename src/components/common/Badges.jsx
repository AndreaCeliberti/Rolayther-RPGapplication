import { seatsLabel, seatsTone, pluralize } from "../../utils/homeSessions";
import { stateLabel } from "../../utils/sessionState";

// Le piccole etichette del sito: stato di una sessione, ruolo, posti, tag.
// L'aspetto sta tutto in styles/ui-kit.css; qui si sceglie solo il significato.
// Il colore rinforza e basta: il senso è nel testo e nella forma del segno davanti.

// forma del segno davanti al testo (il cerchio è quello di base)
const MARK_CLASS = {
  none: "rl-badge--no-mark",
  diamond: "rl-badge--diamond",
  sparkle: "rl-badge--sparkle",
  ring: "rl-badge--ring",
  glow: "rl-badge--glow",
};

// tone: steel (base) · violet · cyan · emerald · amber · rose · muted · plate
export function BaseBadge({ tone = "steel", mark, size, className = "", children, ...rest }) {
  const cls = [
    "rl-badge",
    tone !== "steel" && `rl-badge--${tone}`,
    MARK_CLASS[mark],
    size === "lg" && "rl-badge--lg",
    className,
  ]
    .filter(Boolean)
    .join(" ");

  return (
    <span className={cls} {...rest}>
      {children}
    </span>
  );
}

// Etichetta neutra, senza segno: gioco, genere, conteggi
export function Chip({ children, size, className }) {
  return (
    <BaseBadge mark="none" size={size} className={className}>
      {children}
    </BaseBadge>
  );
}

// ---------------------------------------------------------------------------------------------
// Stato della sessione (0 In arrivo · 1 Pubblicata · 2 Piena · 3 In corso · 4 Completata · 5 Annullata)

const STATE_LOOK = {
  0: { tone: "muted", mark: "ring" },
  1: { tone: "violet" },
  2: { tone: "muted" },
  3: { tone: "cyan", mark: "glow" },
  4: { tone: "emerald" },
  5: { tone: "rose" },
};

export function StateBadge({ state, size, className }) {
  const look = STATE_LOOK[Number(state)] ?? { tone: "muted" };
  return (
    <BaseBadge tone={look.tone} mark={look.mark} size={size} className={className}>
      {stateLabel(state)}
    </BaseBadge>
  );
}

// ---------------------------------------------------------------------------------------------
// Ruolo: il segno cambia forma come i posti al tavolo della Home (cerchio, rombo) e l'Admin è d'acciaio lucido

const ROLE_LOOK = {
  Player: { tone: "steel" },
  Master: { tone: "violet", mark: "diamond" },
  Admin: { tone: "plate", mark: "sparkle" },
};

export function RoleBadge({ role, size, className }) {
  const look = ROLE_LOOK[role] ?? { tone: "steel", mark: "none" };
  return (
    <BaseBadge tone={look.tone} mark={look.mark} size={size} className={className}>
      {role}
    </BaseBadge>
  );
}

// ---------------------------------------------------------------------------------------------
// Posti: `seats` è quello di normalizeSession (utils/homeSessions). Se la sessione è finita
// (completata o annullata) i posti liberi non contano più: si mostra quanti c'erano.

const SEATS_TONE = { open: "emerald", last: "amber", full: "muted" };

export function SeatsBadge({ seats, finished = false, size, className }) {
  if (finished) {
    return (
      <Chip size={size} className={className}>
        {`${seats.taken} ${pluralize(seats.taken, "iscritto", "iscritti")}`}
      </Chip>
    );
  }
  return (
    <BaseBadge tone={SEATS_TONE[seatsTone(seats)]} size={size} className={className}>
      {seatsLabel(seats)}
    </BaseBadge>
  );
}
