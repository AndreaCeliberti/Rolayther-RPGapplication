import { useContext, useRef, useState } from "react";
import { Button, Spinner } from "react-bootstrap";
import { ToastContext } from "../../context/ToastContext";
import { IMAGE_ACCEPT, prepareImage, validateImage } from "../../utils/imageFile";
import { apiErrorMessage } from "../../utils/apiError";

// Pulsante "cambia immagine" con caricamento immediato: scegli il file e parte subito.
// Serve nelle pagine profilo (foto del Player o del Master) e nell'elenco sessioni (cover).
//
//  - onUpload: async (file) => ... ; se lancia, viene mostrato l'errore. Il successo lo gestisce il chiamante
//    (di solito aggiorna lo stato locale con il nuovo URL).
export default function ImageUploadButton({
  label = "Cambia foto",
  onUpload,
  maxDimension = 1600,
  size = "sm",
  variant = "outline-secondary",
  successMessage = "Immagine aggiornata ✅",
  disabled = false,
  className = "",
}) {
  const { showToast } = useContext(ToastContext);
  const inputRef = useRef(null);
  const [busy, setBusy] = useState(false);

  const handlePick = async (e) => {
    const picked = e.target.files?.[0];
    e.target.value = ""; // permette di riscegliere lo stesso file
    if (!picked) return;

    setBusy(true);
    try {
      const prepared = await prepareImage(picked, { maxDimension });
      const problem = validateImage(prepared);
      if (problem) {
        showToast(problem, "danger");
        return;
      }

      await onUpload(prepared);
      showToast(successMessage, "success");
    } catch (err) {
      showToast(apiErrorMessage(err, "Caricamento non riuscito"), "danger");
    } finally {
      setBusy(false);
    }
  };

  return (
    <>
      <Button
        type="button"
        size={size}
        variant={variant}
        className={className}
        disabled={disabled || busy}
        onClick={() => inputRef.current?.click()}
      >
        {busy ? (
          <>
            <Spinner size="sm" className="me-2" />
            Caricamento…
          </>
        ) : (
          label
        )}
      </Button>

      <input
        ref={inputRef}
        type="file"
        accept={IMAGE_ACCEPT}
        className="d-none"
        onChange={handlePick}
        data-testid="image-upload-button-input"
        aria-label={label}
      />
    </>
  );
}
