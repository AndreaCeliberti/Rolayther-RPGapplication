import { useEffect, useRef, useState } from "react";
import { Button, Form, Spinner } from "react-bootstrap";
import { IMAGE_ACCEPT, formatBytes, prepareImage, validateImage } from "../../utils/imageFile";

// Campo "scegli un'immagine" per i form di creazione/modifica: sostituisce i vecchi campi URL.
//
// Il form tiene in uno stato il File scelto (file / onChange) e lo carica DOPO aver salvato i dati,
// perché gli endpoint di upload lavorano su un elemento che esiste già.
//
//  - file:        File scelto (già ridimensionato) oppure null
//  - currentUrl:  immagine già salvata, mostrata come anteprima finché non se ne sceglie un'altra
//  - shape:       "circle" per le foto profilo, "wide" per cover e loghi
//  - maxDimension: lato lungo massimo in pixel (le immagini più grandi vengono ridotte)
export default function ImageUploadField({
  label = "Immagine",
  file,
  onChange,
  currentUrl = null,
  shape = "wide",
  maxDimension = 1600,
  required = false,
  disabled = false,
  hint,
}) {
  const inputRef = useRef(null);
  const [previewUrl, setPreviewUrl] = useState(null);
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);

  // Anteprima del file scelto: l'URL temporaneo va rilasciato quando non serve più
  useEffect(() => {
    if (!file) {
      setPreviewUrl(null);
      return undefined;
    }
    const url = URL.createObjectURL(file);
    setPreviewUrl(url);
    return () => URL.revokeObjectURL(url);
  }, [file]);

  const handlePick = async (e) => {
    const picked = e.target.files?.[0];
    e.target.value = ""; // permette di riscegliere lo stesso file
    if (!picked) return;

    setError("");
    setBusy(true);
    try {
      const prepared = await prepareImage(picked, { maxDimension });
      const problem = validateImage(prepared);
      if (problem) {
        setError(problem);
        return;
      }
      onChange?.(prepared);
    } finally {
      setBusy(false);
    }
  };

  const clearChoice = () => {
    setError("");
    onChange?.(null);
  };

  // Con un file scelto si mostra la sua anteprima; senza, l'immagine già salvata (se c'è)
  const shown = file ? previewUrl || currentUrl : currentUrl;
  const isCircle = shape === "circle";

  const boxStyle = {
    width: isCircle ? 72 : 128,
    height: 72,
    flex: "0 0 auto",
    overflow: "hidden",
    border: "1px solid var(--border)",
    borderRadius: isCircle ? "50%" : 12,
    background: "rgba(10,7,18,.55)",
    display: "flex",
    alignItems: "center",
    justifyContent: "center",
  };

  const buttonLabel = file ? "Cambia file" : shown ? "Sostituisci immagine" : "Scegli immagine";

  return (
    <Form.Group className="mb-2">
      <Form.Label>
        {label}
        {required ? " *" : ""}
      </Form.Label>

      <div className="d-flex align-items-center gap-3 flex-wrap">
        <div style={boxStyle} aria-hidden="true">
          {shown ? (
            <img
              src={shown}
              alt=""
              style={{ width: "100%", height: "100%", objectFit: "cover" }}
            />
          ) : (
            <span style={{ fontSize: 26, opacity: 0.6 }}>{isCircle ? "👤" : "🖼️"}</span>
          )}
        </div>

        <div className="d-flex flex-column gap-1" style={{ minWidth: 0 }}>
          <div className="d-flex gap-2 flex-wrap">
            <Button
              type="button"
              size="sm"
              variant="outline-primary"
              disabled={disabled || busy}
              onClick={() => inputRef.current?.click()}
            >
              {busy ? (
                <>
                  <Spinner size="sm" className="me-2" />
                  Preparazione…
                </>
              ) : (
                buttonLabel
              )}
            </Button>

            {file && (
              <Button
                type="button"
                size="sm"
                variant="outline-secondary"
                disabled={disabled || busy}
                onClick={clearChoice}
              >
                Annulla scelta
              </Button>
            )}
          </div>

          <small style={{ color: "var(--muted)" }} className="text-break">
            {file
              ? `${file.name} · ${formatBytes(file.size)}`
              : hint || "JPG, PNG, WebP o GIF. Le foto grandi vengono ridimensionate in automatico."}
          </small>

          {error && (
            <small className="text-danger" role="alert">
              {error}
            </small>
          )}
        </div>
      </div>

      <input
        ref={inputRef}
        type="file"
        accept={IMAGE_ACCEPT}
        className="d-none"
        onChange={handlePick}
        disabled={disabled}
        data-testid="image-upload-input"
        aria-label={label}
      />
    </Form.Group>
  );
}
