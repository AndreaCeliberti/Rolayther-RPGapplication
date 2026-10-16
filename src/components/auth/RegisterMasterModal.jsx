import { useContext, useMemo, useState } from "react";
import { Modal, Button, Form, Row, Col, Spinner } from "react-bootstrap";
import api from "../../api/axios";
import { ToastContext } from "../../context/ToastContext";
import ImageUploadField from "../common/ImageUploadField";
import { uploadAvatarAfterRegistration } from "../../api/upload";

const initialState = {
  name: "",
  surname: "",
  nickName: "",
  dateOfBirth: "", // yyyy-mm-dd
  bioMaster: "",
  email: "",
  password: "",
  confirmPassword: "",
};

// onRegistered({ email, emailSent, isMaster }): a richiesta inviata l'app apre la finestra "Controlla la tua email".
// Senza onRegistered si torna al vecchio comportamento (si apre il login).
export default function RegisterMasterModal({ show, handleClose, openLogin, onRegistered }) {
  const { showToast } = useContext(ToastContext);

  const [form, setForm] = useState(initialState);
  const [saving, setSaving] = useState(false);
  const [validated, setValidated] = useState(false);
  const [pwMismatch, setPwMismatch] = useState(false);
  const [avatarFile, setAvatarFile] = useState(null);

  const canSubmit = useMemo(() => {
    return (
      form.name &&
      form.surname &&
      form.nickName &&
      form.dateOfBirth &&
      form.email &&
      form.password &&
      form.confirmPassword &&
      form.password === form.confirmPassword
    );
  }, [form]);

  const onChange = (e) => {
    const { name, value } = e.target;
    setForm((f) => ({ ...f, [name]: value }));

    if (name === "password" || name === "confirmPassword") {
      const next = { ...form, [name]: value };
      setPwMismatch(
        next.password.length > 0 &&
          next.confirmPassword.length > 0 &&
          next.password !== next.confirmPassword
      );
    }
  };

  const resetAndClose = () => {
    setForm(initialState);
    setValidated(false);
    setPwMismatch(false);
    setAvatarFile(null);
    setSaving(false);
    handleClose();
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    e.stopPropagation();

    setValidated(true);

    if (!canSubmit) {
      showToast("Compila tutti i campi obbligatori correttamente", "danger");
      return;
    }

    setSaving(true);

    try {
      const payload = {
        name: form.name,
        surname: form.surname,
        nickName: form.nickName,
        dateOfBirth: form.dateOfBirth, // DateOnly
        bioMaster: form.bioMaster || null,
        email: form.email,
        password: form.password,
        confirmPassword: form.confirmPassword,
      };

      const res = await api.post("/Auth/register-master", payload);

      // emailSent false = richiesta registrata ma l'email di conferma non è partita (da lì si può chiedere un nuovo invio)
      const emailSent = res.data?.emailSent !== false;
      const registeredEmail = form.email;

      // La foto si carica dopo la registrazione (serve un account esistente).
      // Se il caricamento fallisce la registrazione resta valida: si può riprovare dal profilo.
      const photoProblem = await uploadAvatarAfterRegistration({
        role: "master",
        email: form.email,
        password: form.password,
        file: avatarFile,
      });

      if (photoProblem) {
        showToast(
          `Richiesta inviata, ma la foto non è stata caricata (${photoProblem}). Potrai aggiungerla dal tuo profilo quando sarai approvato.`,
          "warning",
          6000
        );
      } else if (!onRegistered) {
        showToast(
          "Richiesta inviata ✅ Conferma la tua email: un amministratore deve approvarla prima che tu possa creare sessioni.",
          "success",
          6000
        );
      }
      resetAndClose();

      if (onRegistered) onRegistered({ email: registeredEmail, emailSent, isMaster: true });
      else openLogin?.();
    } catch (err) {
      const msg =
        err?.response?.data?.message ||
        err?.response?.data?.Message ||
        "Registrazione fallita. Controlla i dati e riprova.";
      showToast(msg, "danger");
    } finally {
      setSaving(false);
    }
  };

  return (
    <Modal
      show={show}
      onHide={resetAndClose}
      centered
      animation
      backdrop="static"
      contentClassName="border-0 shadow"
    >
      <Modal.Header closeButton className="border-0 pb-0 text-white">
        <Modal.Title className="fw-semibold">Registrazione Master</Modal.Title>
      </Modal.Header>

      <Modal.Body className="pt-2 text-white">
        <p className=" small mb-3 ">
          Crea il tuo profilo Master per pubblicare e gestire sessioni.
        </p>

        <Form noValidate validated={validated} onSubmit={handleSubmit}>
          <Row className="g-2">
            <Col xs={12} md={6}>
              <Form.Group className="mb-2">
                <Form.Label>Nome *</Form.Label>
                <Form.Control
                  name="name"
                  value={form.name}
                  onChange={onChange}
                  required
                  placeholder="Luca"
                />
                <Form.Control.Feedback type="invalid">
                  Inserisci il nome.
                </Form.Control.Feedback>
              </Form.Group>
            </Col>

            <Col xs={12} md={6}>
              <Form.Group className="mb-2">
                <Form.Label>Cognome *</Form.Label>
                <Form.Control
                  name="surname"
                  value={form.surname}
                  onChange={onChange}
                  required
                  placeholder="Bianchi"
                />
                <Form.Control.Feedback type="invalid">
                  Inserisci il cognome.
                </Form.Control.Feedback>
              </Form.Group>
            </Col>
          </Row>

          <Row className="g-2">
            <Col xs={12} md={6}>
              <Form.Group className="mb-2">
                <Form.Label>Nickname *</Form.Label>
                <Form.Control
                  name="nickName"
                  value={form.nickName}
                  onChange={onChange}
                  required
                  placeholder="DungeonMaster"
                />
                <Form.Control.Feedback type="invalid">
                  Inserisci un nickname.
                </Form.Control.Feedback>
              </Form.Group>
            </Col>

            <Col xs={12} md={6}>
              <Form.Group className="mb-2">
                <Form.Label>Data di nascita *</Form.Label>
                <Form.Control
                  type="date"
                  name="dateOfBirth"
                  value={form.dateOfBirth}
                  onChange={onChange}
                  required
                />
                <Form.Control.Feedback type="invalid">
                  Seleziona una data valida.
                </Form.Control.Feedback>
              </Form.Group>
            </Col>
          </Row>

          <ImageUploadField
            label="Foto profilo"
            shape="circle"
            maxDimension={512}
            file={avatarFile}
            onChange={setAvatarFile}
            disabled={saving}
          />

          <Form.Group className="mb-3">
            <Form.Label>Bio</Form.Label>
            <Form.Control
              as="textarea"
              rows={2}
              name="bioMaster"
              value={form.bioMaster}
              onChange={onChange}
              placeholder="Presentati come Master..."
            />
          </Form.Group>

          <hr className="my-3" />

          <Form.Group className="mb-2">
            <Form.Label>Email *</Form.Label>
            <Form.Control
              type="email"
              name="email"
              value={form.email}
              onChange={onChange}
              required
              placeholder="master@esempio.com"
            />
            <Form.Control.Feedback type="invalid">
              Inserisci un’email valida.
            </Form.Control.Feedback>
          </Form.Group>

          <Row className="g-2">
            <Col xs={12} md={6}>
              <Form.Group className="mb-2">
                <Form.Label>Password *</Form.Label>
                <Form.Control
                  type="password"
                  name="password"
                  value={form.password}
                  onChange={onChange}
                  required
                  minLength={8}
                />
                <Form.Control.Feedback type="invalid">
                  Minimo 8 caratteri.
                </Form.Control.Feedback>
              </Form.Group>
            </Col>

            <Col xs={12} md={6}>
              <Form.Group className="mb-2">
                <Form.Label>Conferma Password *</Form.Label>
                <Form.Control
                  type="password"
                  name="confirmPassword"
                  value={form.confirmPassword}
                  onChange={onChange}
                  required
                  isInvalid={pwMismatch}
                />
                <Form.Control.Feedback type="invalid">
                  {pwMismatch ? "Le password non coincidono." : "Conferma la password."}
                </Form.Control.Feedback>
              </Form.Group>
            </Col>
          </Row>

          <Button
            type="submit"
            className="w-100 mt-2"
            disabled={saving || !canSubmit}
          >
            {saving ? (
              <>
                <Spinner size="sm" className="me-2" />
                Registrazione...
              </>
            ) : (
              "Registrati"
            )}
          </Button>

          <div className="text-center mt-3">
            <small className="text-white">
              Hai già un account?{" "}
              <span
                className="text-primary"
                style={{ cursor: "pointer" }}
                onClick={() => {
                  resetAndClose();
                  openLogin?.();
                }}
              >
                Accedi
              </span>
            </small>
          </div>
        </Form>
      </Modal.Body>
    </Modal>
  );
}
