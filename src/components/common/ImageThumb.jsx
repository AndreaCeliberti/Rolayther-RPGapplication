import { useState } from "react";

// Miniatura di un'immagine (cover, logo, foto). Senza URL — o se l'immagine non si carica — mostra un segnaposto.
export default function ImageThumb({
  url,
  width = 44,
  height = 44,
  radius = 10,
  placeholder = "🖼️",
}) {
  // Si ricorda quale URL non si è caricato: se l'URL cambia (nuova immagine) si riprova a mostrarla
  const [failedUrl, setFailedUrl] = useState(null);

  const showImage = Boolean(url) && failedUrl !== url;

  return (
    <span
      aria-hidden="true"
      style={{
        width,
        height,
        flex: "0 0 auto",
        overflow: "hidden",
        borderRadius: radius,
        border: "1px solid var(--border)",
        background: "rgba(10,7,18,.55)",
        display: "inline-flex",
        alignItems: "center",
        justifyContent: "center",
        fontSize: Math.round(Math.min(width, height) * 0.42),
      }}
    >
      {showImage ? (
        <img
          src={url}
          alt=""
          onError={() => setFailedUrl(url)}
          style={{ width: "100%", height: "100%", objectFit: "cover" }}
        />
      ) : (
        <span style={{ opacity: 0.6 }}>{placeholder}</span>
      )}
    </span>
  );
}
