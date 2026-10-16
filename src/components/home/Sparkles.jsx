// Le stelline dell'intestazione delle email, in sottofondo all'hero: stessa famiglia visiva, nessun peso (SVG fisso, niente animazioni)

const STARS = [
  [70, 90, 1.1],
  [180, 210, 0.7],
  [120, 420, 0.9],
  [310, 60, 0.6],
  [430, 330, 0.8],
  [560, 120, 1],
  [640, 520, 0.7],
  [740, 70, 0.9],
  [880, 220, 0.6],
  [960, 470, 1.1],
  [1040, 110, 0.8],
  [1110, 330, 0.7],
  [1150, 560, 0.9],
  [820, 590, 0.6],
];

export default function Sparkles() {
  return (
    <svg className="hm-sparkles" viewBox="0 0 1200 640" preserveAspectRatio="xMidYMid slice" aria-hidden="true" focusable="false">
      {STARS.map(([x, y, s], i) => (
        <path
          key={i}
          transform={`translate(${x} ${y}) scale(${s})`}
          d="M0 -7 L1.6 -1.6 L7 0 L1.6 1.6 L0 7 L-1.6 1.6 L-7 0 L-1.6 -1.6 Z"
          fill="#c4b5fd"
          opacity={0.18 + (i % 4) * 0.1}
        />
      ))}
    </svg>
  );
}
