import { useContext, useMemo, useState } from "react";
import { Modal, Button, Form, Row, Col, Spinner } from "react-bootstrap";
import api from "../../api/axios";
import { ToastContext } from "../../context/ToastContext";

const initialState = {
  name: "",
  surname: "",
  nickName: "",
  dateOfBirth: "", // yyyy-mm-dd
  avatarImgUrl: "",
  bioMaster: "",
  email: "",
  password: "",
  confirmPassword: "",
};

export default function RegisterMasterModal({ show, handleClose, openLogin }) {
  const { showToast } = useContext(ToastContext);

  const [form, setForm] = useState(initialState);
  const [saving, setSaving] = useState(false);
  const [validated, setValidated] = useState(false);
  const [pwMismatch, setPwMismatch] = useState(false);

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
        avatarImgUrl: form.avatarImgUrl || null,
        bioMaster: form.bioMaster || null,
        email: form.email,
        password: form.password,
        confirmPassword: form.confirmPassword,
      };

      await api.post("/Auth/register-master", payload);

      showToast("Creazione Master completata", "success");
      resetAndClose();
      //openLogin?.();
    } catch (err) {
      const msg =
        err?.response?.data?.message ||
        err?.response?.data?.Message ||
        "Creazione fallita. Controlla i dati e riprova.";
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
        <Modal.Title className="fw-semibold">Creazione Master</Modal.Title>
      </Modal.Header>

      <Modal.Body className="pt-2 text-white">
        <p className=" small mb-3 ">
          Crea un profilo master.
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

          <Form.Group className="mb-2">
            <Form.Label>Avatar URL</Form.Label>
            <Form.Control
              name="avatarImgUrl"
              value={form.avatarImgUrl}
              onChange={onChange}
              placeholder="https://..."
            />
          </Form.Group>

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
              "Crea"
            )}
          </Button>

          
        </Form>
      </Modal.Body>
    </Modal>
  );
}
