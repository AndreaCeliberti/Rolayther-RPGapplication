import { useContext, useEffect, useMemo, useState } from "react";
import { Modal, Button, Form, Spinner } from "react-bootstrap";
import { ToastContext } from "../../context/ToastContext";
import { AuthContext } from "../../context/AuthContext";
import { PlatformsApi } from "../../api/platforms.api";
import ImageUploadField from "../common/ImageUploadField";
import { uploadAfterSave } from "../../utils/apiError";

const initial = {
  name: "",
  description: "",
};

export default function CreatePlatformModal({ show, handleClose, onCreated }) {
  const { showToast } = useContext(ToastContext);
  const { user } = useContext(AuthContext);

  const isAdmin = user?.role === "Admin";

  const [form, setForm] = useState(initial);
  const [validated, setValidated] = useState(false);
  const [saving, setSaving] = useState(false);
  const [logoFile, setLogoFile] = useState(null);

  useEffect(() => {
    if (!show) {
      setForm(initial);
      setValidated(false);
      setSaving(false);
      setLogoFile(null);
    }
  }, [show]);

  const canSubmit = useMemo(() => {
    return Boolean(isAdmin && form.name.trim());
  }, [isAdmin, form]);

  const onChange = (e) => {
    const { name, value } = e.target;
    setForm((f) => ({ ...f, [name]: value }));
  };

  const closeAndReset = () => {
    setForm(initial);
    setValidated(false);
    setSaving(false);
    setLogoFile(null);
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
      showToast("Compila i campi obbligatori", "danger");
      return;
    }

    setSaving(true);
    try {
      const res = await PlatformsApi.create({
        name: form.name.trim(),
        description: form.description?.trim() ? form.description.trim() : null,
      });

      // Il logo (facoltativo) si carica subito dopo la creazione (serve l'id della nuova piattaforma).
      const newPlatformId = res?.data?.platformId ?? res?.data?.PlatformId;
      const logoProblem = await uploadAfterSave(logoFile, newPlatformId, PlatformsApi.uploadLogo);

      if (logoProblem) {
        showToast(
          `Piattaforma creata, ma il logo non è stato caricato (${logoProblem}). Puoi aggiungerlo dalla tabella.`,
          "warning"
        );
      } else {
        showToast("Piattaforma creata ✅", "success");
      }
      closeAndReset();
      onCreated?.();
    } catch (err) {
      const msg =
        err?.response?.data?.message ||
        err?.response?.data?.Message ||
        "Creazione piattaforma fallita";
      showToast(msg, "danger");
    } finally {
      setSaving(false);
    }
  };

  return (
    <Modal show={show} onHide={closeAndReset} centered animation backdrop="static">
      <Modal.Header className="text-white" closeButton>
        <Modal.Title> Crea Piattaforma</Modal.Title>
      </Modal.Header>

      <Modal.Body className="text-white">
        {!isAdmin ? (
          <div className="text-muted">
            <div className="fw-semibold mb-1">Accesso negato</div>
            Solo gli Admin possono creare piattaforme.
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
                placeholder="Es. Discord"
              />
              <Form.Control.Feedback type="invalid">
                Inserisci un nome.
              </Form.Control.Feedback>
            </Form.Group>

            <Form.Group className="mb-3">
              <Form.Label>Descrizione</Form.Label>
              <Form.Control
                as="textarea"
                rows={2}
                name="description"
                value={form.description}
                onChange={onChange}
                placeholder="Facoltativa…"
              />
            </Form.Group>

            <div className="mb-3">
              <ImageUploadField
                label="Logo"
                maxDimension={512}
                file={logoFile}
                onChange={setLogoFile}
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
