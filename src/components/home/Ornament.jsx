// Il filetto con il rombo al centro che chiude l'intestazione delle email: qui separa l'hero dal resto della pagina

export default function Ornament() {
  return (
    <div className="hm-ornament" aria-hidden="true">
      <svg viewBox="0 0 600 24" preserveAspectRatio="xMidYMid meet" focusable="false">
        <defs>
          <linearGradient id="hm-orn-line" x1="0" y1="0" x2="1" y2="0">
            <stop offset="0" stopColor="#8b5cf6" stopOpacity="0" />
            <stop offset=".5" stopColor="#a78bfa" stopOpacity=".75" />
            <stop offset="1" stopColor="#8b5cf6" stopOpacity="0" />
          </linearGradient>
        </defs>
        <rect x="0" y="11.5" width="600" height="1" fill="url(#hm-orn-line)" />
        <path d="M300 4 L308 12 L300 20 L292 12 Z" fill="none" stroke="#c4b5fd" strokeWidth="1.4" />
        <path d="M300 8.5 L303.5 12 L300 15.5 L296.5 12 Z" fill="#a78bfa" />
        <path d="M274 12 L278 8 L282 12 L278 16 Z" fill="#8b5cf6" opacity=".7" />
        <path d="M318 12 L322 8 L326 12 L322 16 Z" fill="#8b5cf6" opacity=".7" />
      </svg>
    </div>
  );
}
