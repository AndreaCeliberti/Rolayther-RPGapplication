import { useId } from "react";
import { initialOf } from "../../utils/homeSessions";

// Il tavolo visto dall'alto: il Master siede in cima, i giocatori attorno.
// Posto occupato = pieno, posto libero = tratteggiato. È il disegno che mostra a colpo d'occhio quanta strada manca al "completo".

const C = 200; // centro del disegno (viewBox 400 x 400)

// "lg" è il tavolo grande dell'hero (con data al centro, iniziali e foto); "sm" è quello piccolo delle righe,
// dove i posti sono più grandi in proporzione e i tratti più spessi, altrimenti a quella misura non si vedrebbero.
const GEOMETRY = {
  lg: { table: 96, ring: 148, seat: 27, master: 29, stroke: 2, ringGap: 8 },
  sm: { table: 72, ring: 136, seat: 38, master: 36, stroke: 5, ringGap: 12 },
};

function polar(angleDeg, radius) {
  const a = (angleDeg * Math.PI) / 180;
  return { x: C + radius * Math.cos(a), y: C + radius * Math.sin(a) };
}

// seats: [{ filled, name, avatar }]   master: { name, avatar } | null
// center: { day, month, time } | null (solo nel tavolo grande)
// tone: "open" | "last" | "full"  (il posto libero da prendere diventa giallo quando è l'ultimo)
// label: descrizione per chi non vede il disegno; se manca, il disegno è solo decorativo
export default function TableSeats({ seats, master = null, center = null, size = "lg", tone = "open", label, animate = false }) {
  const uid = useId().replace(/:/g, "");
  const g = GEOMETRY[size];
  const big = size === "lg";

  const step = 360 / (seats.length + 1);
  // con molti posti si rimpiccioliscono, così non si toccano
  const seatR = Math.max(10, Math.min(g.seat, Math.floor(((Math.PI * g.ring) / (seats.length + 1)) * 0.7)));
  const masterAvatarR = g.master - 5;
  const nextFree = seats.findIndex((s) => !s.filled);
  const top = polar(-90, g.ring);

  return (
    <svg
      className={`hm-table hm-table--${size}${animate ? " is-animated" : ""}`}
      viewBox="0 0 400 400"
      {...(label ? { role: "img", "aria-label": label } : { "aria-hidden": true, focusable: "false" })}
    >
      <defs>
        <radialGradient id={`${uid}-halo`} cx="50%" cy="50%" r="50%">
          <stop offset="0" stopColor="#8b5cf6" stopOpacity=".34" />
          <stop offset="1" stopColor="#8b5cf6" stopOpacity="0" />
        </radialGradient>
        <radialGradient id={`${uid}-disc`} cx="50%" cy="36%" r="72%">
          <stop offset="0" stopColor="#3f2a7a" />
          <stop offset=".62" stopColor="#1c1235" />
          <stop offset="1" stopColor="#120b24" />
        </radialGradient>
        <linearGradient id={`${uid}-seat`} x1="0" y1="0" x2="1" y2="1">
          <stop offset="0" stopColor="#c4b5fd" />
          <stop offset="1" stopColor="#7c3aed" />
        </linearGradient>
        <linearGradient id={`${uid}-master`} x1="0" y1="0" x2="1" y2="1">
          <stop offset="0" stopColor="#f5f3ff" />
          <stop offset="1" stopColor="#a78bfa" />
        </linearGradient>
        <clipPath id={`${uid}-clip`}>
          <circle r={seatR} />
        </clipPath>
        <clipPath id={`${uid}-clip-master`}>
          <circle r={masterAvatarR} />
        </clipPath>
      </defs>

      <circle cx={C} cy={C} r={g.ring + g.seat + 10} fill={`url(#${uid}-halo)`} />

      <g className="hm-table__top">
        <circle cx={C} cy={C} r={g.table} fill={`url(#${uid}-disc)`} stroke="#5b43a8" strokeOpacity=".8" strokeWidth={g.stroke} />
        {big && (
          <circle
            cx={C}
            cy={C}
            r={g.table - 11}
            fill="none"
            stroke="#a78bfa"
            strokeOpacity=".3"
            strokeWidth="2"
            strokeLinecap="round"
            strokeDasharray="1 8"
          />
        )}
      </g>

      {big && center && (
        <g className="hm-table__stamp" aria-hidden="true">
          <text x={C} y="194" textAnchor="middle" className="hm-table__day">
            {center.day}
          </text>
          <text x={C} y="225" textAnchor="middle" className="hm-table__month">
            {center.month}
          </text>
          <text x={C} y="253" textAnchor="middle" className="hm-table__time">
            {center.time}
          </text>
        </g>
      )}

      {/* il Master, in cima: un rombo chiaro, così si distingue dai giocatori */}
      <g transform={`translate(${top.x} ${top.y})`}>
        <g className="hm-seat is-master" style={{ "--i": 0 }}>
          <rect
            x={-g.master}
            y={-g.master}
            width={g.master * 2}
            height={g.master * 2}
            rx={g.master * 0.34}
            transform="rotate(45)"
            fill={`url(#${uid}-master)`}
            stroke="#ede9fe"
            strokeOpacity=".8"
            strokeWidth={g.stroke}
          />
          {big && master?.avatar && (
            <image
              href={master.avatar}
              x={-masterAvatarR}
              y={-masterAvatarR}
              width={masterAvatarR * 2}
              height={masterAvatarR * 2}
              preserveAspectRatio="xMidYMid slice"
              clipPath={`url(#${uid}-clip-master)`}
            />
          )}
          {big && !master?.avatar && master?.name && (
            <text className="hm-seat__initial is-master" textAnchor="middle" dominantBaseline="central" y="1">
              {initialOf(master.name)}
            </text>
          )}
        </g>
      </g>

      {seats.map((seat, i) => {
        const p = polar(-90 + step * (i + 1), g.ring);
        const isNext = i === nextFree && tone !== "full";

        return (
          <g key={i} transform={`translate(${p.x} ${p.y})`}>
            <g
              className={`hm-seat ${seat.filled ? "is-taken" : "is-free"}${isNext ? ` is-next is-${tone}` : ""}`}
              style={{ "--i": i + 1 }}
            >
              {seat.filled ? (
                <>
                  <circle r={seatR} fill={`url(#${uid}-seat)`} stroke="#ede9fe" strokeOpacity=".5" strokeWidth={g.stroke * 0.75} />
                  {big && seat.avatar && (
                    <image
                      href={seat.avatar}
                      x={-seatR}
                      y={-seatR}
                      width={seatR * 2}
                      height={seatR * 2}
                      preserveAspectRatio="xMidYMid slice"
                      clipPath={`url(#${uid}-clip)`}
                    />
                  )}
                  {big && !seat.avatar && seat.name && (
                    <text className="hm-seat__initial" textAnchor="middle" dominantBaseline="central" y="1">
                      {initialOf(seat.name)}
                    </text>
                  )}
                </>
              ) : (
                <>
                  {isNext && <circle className="hm-seat__ring" r={seatR + g.ringGap} fill="none" strokeWidth={g.stroke} />}
                  <circle className="hm-seat__empty" r={seatR} strokeWidth={g.stroke} strokeLinecap="round" />
                </>
              )}
            </g>
          </g>
        );
      })}
    </svg>
  );
}
