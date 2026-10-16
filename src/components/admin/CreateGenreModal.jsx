import { useContext, useEffect, useMemo, useState } from "react";
import { Modal, Button, Form, Spinner } from "react-bootstrap";
import { ToastContext } from "../../context/ToastContext";
import { AuthContext } from "../../context/AuthContext";
import { GenresApi } from "../../api/genres.api";
import ImageUploadField from "../common/ImageUploadField";
import { uploadAfterSave } from "../../utils/apiError";

const initial = {
  name: "",
  description: "",
};

export default function CreateGenreModal({ show, handleClose, onCreated }) {
  const { showToast } = useContext(ToastContext);
  const { user } = useContext(AuthContext);

  const isAdmin = user?.role === "Admin";

  const [form, setForm] = useState(initial);
  const [validated, setValidated] = useState(false);
  const [saving, setSaving] = useState(false);
  const [imageFile, setImageFile] = useState(null);

  useEffect(() => {
    if (!show) {
      setForm(initial);
      setValidated(false);
      setSaving(false);
      setImageFile(null);
    }
  }, [show]);

  const canSubmit = useMemo(() => {
    // L'immagine è obbligatoria come prima (prima era un URL, ora è un file)
    return Boolean(isAdmin && form.name.trim() && form.description.trim() && imageFile);
  }, [isAdmin, form, imageFile]);

  const onChange = (e) => {
    const { name, value } = e.target;
    setForm((f) => ({ ...f, [name]: value }));
  };

  const closeAndReset = () => {
    setForm(initial);
    setValidated(false);
    setSaving(false);
    setImageFile(null);
    handleClose();
  };

  const onSubmit = async (e) => {
    e.preventDefault();
    e.stopPropagation();
    setValidated(true);

    if (!isAdmin) {
      showToast("Accesso negato: solo Admin", "danger");
      return;
    }

    if (!canSubmit) {
      showToast("Compila tutti i campi obbligatori", "danger");
      return;
    }

    setSaving(true);
    try {
      const res = await GenresApi.create({
        name: form.name.trim(),
        description: form.description.trim(),
      });

      // L'immagine si carica subito dopo la creazione (serve l'id del nuovo genere).
      // Se il caricamento fallisce il genere resta creato: l'immagine si può aggiungere dalla tabella.
      const newGenreId = res?.data?.genreId ?? res?.data?.GenreId;
      const imageProblem = await uploadAfterSave(imageFile, newGenreId, GenresApi.uploadImage);

      if (imageProblem) {
        showToast(
          `Genere creato, ma l'immagine non è stata caricata (${imageProblem}). Puoi aggiungerla dalla tabella.`,
          "warning"
        );
      } else {
        showToast("Genere creato ✅", "success");
      }
      closeAndReset();
      onCreated?.();
    } catch (err) {
      const msg =
        err?.response?.data?.message ||
        err?.response?.data?.Message ||
        "Creazione genere fallita";
      showToast(msg, "danger");
    } finally {
      setSaving(false);
    }
  };

  return (
    <Modal show={show} onHide={closeAndReset} centered animation backdrop="static">
      <Modal.Header closeButton className="text-white">
        <Modal.Title>+ Crea Genere</Modal.Title>
      </Modal.Header>

      <Modal.Body className="text-white">
        {!isAdmin ? (
          <div className="text-white">
            <div className="fw-semibold mb-1">Accesso negato</div>
            Solo gli Admin possono creare generi.
          </div>
        ) : (
          <Form noValidate validated={validated} onSubmit={onSubmit}>
            <Form.Group className="mb-3">
              <Form.Label>Nome *</Form.Label>
              <Form.Control
                name="name"
                value={form.name}
                onChange={onChange}
                required
              />
              <Form.Control.Feedback type="invalid">
                Inserisci un nome.
              </Form.Control.Feedback>
            </Form.Group>

            <Form.Group className="mb-3">
              <Form.Label>Descrizione *</Form.Label>
              <Form.Control
                as="textarea"
                rows={3}
                name="description"
                value={form.description}
                onChange={onChange}
                required
              />
              <Form.Control.Feedback type="invalid">
                Inserisci una descrizione.
              </Form.Control.Feedback>
            </Form.Group>

            <div className="mb-3">
              <ImageUploadField
                label="Immagine"
                required
                maxDimension={1600}
                file={imageFile}
                onChange={setImageFile}
                disabled={saving}
              />
            </div>

            <div className="d-flex justify-content-end gap-2">
              <Button variant="outline-secondary" onClick={closeAndReset}>
                Annulla
              </Button>
              <Button type="submit" disabled={!canSubmit || saving}>
                {saving ? (
                  <>
                    <Spinner size="sm" className="me-2" />
                    Salvataggio…
                  </>
                ) : (
                  "Crea"
                )}
              </Button>
            </div>
          </Form>
        )}
      </Modal.Body>
    </Modal>
  );
}
