// Utilità per le immagini caricate dagli utenti (foto profilo, cover, loghi).
//
// Il backend accetta JPG, PNG, WebP e GIF fino a 5 MB. Qui controlliamo il file prima di inviarlo e,
// per le foto grandi (tipiche dei telefoni: 3-8 MB), le ridimensioniamo direttamente nel browser:
// partono più leggere, passano sempre il limite e occupano meno spazio sul server.

// Formati ammessi (devono combaciare con le estensioni accettate dal backend).
// Elencarli per esteso aiuta anche iPhone/Safari a convertire le foto HEIC in JPG al momento della scelta.
export const IMAGE_ACCEPT = "image/jpeg,image/png,image/webp,image/gif";

const ALLOWED_TYPES = ["image/jpeg", "image/png", "image/webp", "image/gif"];

// Il server rifiuta le richieste oltre i 5.000.000 byte (intestazioni multipart comprese):
// restiamo sotto con un po' di margine.
export const MAX_IMAGE_BYTES = 4_800_000;

// Le immagini più leggere di così, e già nelle dimensioni giuste, partono senza essere ricompresse.
const RECOMPRESS_ABOVE_BYTES = 800_000;

export function formatBytes(bytes) {
  if (bytes < 1_000_000) return `${Math.max(1, Math.round(bytes / 1000))} KB`;
  return `${(bytes / 1_000_000).toFixed(1).replace(".", ",")} MB`;
}

// Torna null se il file va bene, altrimenti un messaggio pronto da mostrare all'utente.
export function validateImage(file) {
  if (!file) return "Nessun file selezionato.";

  if (!ALLOWED_TYPES.includes(file.type)) {
    return "Formato non supportato: usa un'immagine JPG, PNG, WebP o GIF.";
  }

  if (file.size > MAX_IMAGE_BYTES) {
    return `L'immagine pesa ${formatBytes(file.size)}: il massimo è ${formatBytes(MAX_IMAGE_BYTES)}.`;
  }

  return null;
}

function canvasToBlob(canvas, type, quality) {
  return new Promise((resolve) => canvas.toBlob(resolve, type, quality));
}

function replaceExtension(name, ext) {
  const base = (name || "immagine").replace(/\.[^./\\]+$/, "") || "immagine";
  return `${base}.${ext}`;
}

// Ridimensiona (lato lungo <= maxDimension) e ricomprime le immagini grandi.
// Se qualcosa non va (formato non decodificabile, GIF animate, nessun vantaggio) restituisce il file originale:
// sarà la validazione, o il server, a pronunciarsi.
export async function prepareImage(file, { maxDimension = 1600 } = {}) {
  if (!file || !ALLOWED_TYPES.includes(file.type)) return file;

  // Le GIF si lasciano intatte: ricomprimerle toglierebbe l'animazione.
  if (file.type === "image/gif") return file;

  let bitmap;
  try {
    bitmap = await createImageBitmap(file);
  } catch {
    return file;
  }

  try {
    const { width, height } = bitmap;
    const scale = Math.min(1, maxDimension / Math.max(width, height));
    const needsResize = scale < 1;

    if (!needsResize && file.size <= RECOMPRESS_ABOVE_BYTES) return file;

    const canvas = document.createElement("canvas");
    canvas.width = Math.max(1, Math.round(width * scale));
    canvas.height = Math.max(1, Math.round(height * scale));
    canvas.getContext("2d").drawImage(bitmap, 0, 0, canvas.width, canvas.height);

    // JPEG resta JPEG; PNG e WebP diventano WebP (mantiene la trasparenza). Se il browser non sa
    // produrre WebP restituisce un PNG: l'estensione segue il tipo effettivo del risultato.
    const preferredType = file.type === "image/jpeg" ? "image/jpeg" : "image/webp";
    const blob = await canvasToBlob(canvas, preferredType, 0.85);

    if (!blob || blob.size >= file.size) return file;

    const ext = blob.type === "image/jpeg" ? "jpg" : blob.type === "image/webp" ? "webp" : "png";
    return new File([blob], replaceExtension(file.name, ext), {
      type: blob.type,
      lastModified: Date.now(),
    });
  } catch {
    return file;
  } finally {
    bitmap.close?.();
  }
}
