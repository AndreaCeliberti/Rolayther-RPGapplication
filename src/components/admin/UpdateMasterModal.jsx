import { useContext, useEffect, useMemo, useState } from "react";
import { Modal, Button, Form, Row, Col, Spinner } from "react-bootstrap";
import { ToastContext } from "../../context/ToastContext";
import { AuthContext } from "../../context/AuthContext";
import { MastersApi } from "../../api/masters.api";
import ImageUploadField from "../common/ImageUploadField";
import { uploadAfterSave } from "../../utils/apiError";

const empty = {
  name: "",
  surname: "",
  nickName: "",
  dateOfBirth: "", // yyyy-mm-dd
  avatarImgUrl: "", // URL attuale della foto (si cambia caricando un file)
  email: "",
  bioMaster: "",
  password: "",
  confirmPassword: "",
};

function dateOnlyToInput(value) {
  if (!value) return "";
  if (typeof value === "string" && /^\d{4}-\d{2}-\d{2}$/.test(value)) return value;
  const d = new Date(value);
  if (Number.isNaN(d.getTime())) return "";
  const pad = (n) => String(n).padStart(2, "0");
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
}

export default function UpdateMasterModal({
  show,
  handleClose,
  onUpdated,
  master,
}) {
  const { showToast } = useContext(ToastContext);
  const { user } = useContext(AuthContext);
  const isAdmin = user?.role === "Admin";

  const [form, setForm] = useState(empty);
  const [validated, setValidated] = useState(false);
  const [saving, setSaving] = useState(false);
  const [avatarFile, setAvatarFile] = useState(null);

  const [loadingFull, setLoadingFull] = useState(false);

  const closeAndReset = () => {
    setValidated(false);
    setSaving(false);
    setLoadingFull(false);
    setAvatarFile(null);
    setForm(empty);
    handleClose();
  };

  useEffect(() => {
    if (!show) return;
    if (!master) return;

    setAvatarFile(null);
    setForm((prev) => ({
      ...prev,
      name: master.name ?? "",
      surname: master.surname ?? "",
      nickName: master.nickName ?? "",
      dateOfBirth: dateOnlyToInput(master.dateOfBirth),
      avatarImgUrl: master.avatarImgUrl ?? "",
      email: master.email ?? "",
      bioMaster: master.bioMaster ?? "",
      password: "",
      confirmPassword: "",
    }));

    const fetchFull = async () => {
      setLoadingFull(true);
      try {
        const res = await MastersApi.getById(master.masterId);
        const m = res.data;
        setForm((prev) => ({
          ...prev,
          name: m.name ?? "",
          surname: m.surname ?? "",
          nickName: m.nickName ?? "",
          dateOfBirth: dateOnlyToInput(m.dateOfBirth),
          avatarImgUrl: m.avatarImgUrl ?? "",
          email: m.email ?? "",
          bioMaster: m.bioMaster ?? "",
          password: "",
          confirmPassword: "",
        }));
      } catch {
        // ignore
      } finally {
        setLoadingFull(false);
      }
    };

    fetchFull();
  }, [show, master]);

  const passwordTouched = useMemo(
    () => Boolean(form.password?.trim() || form.confirmPassword?.trim()),
    [form.password, form.confirmPassword]
  );

  const passwordTooShort = useMemo(() => {
    const p = form.password?.trim();
    if (!p) return false; // se vuota ok
    return p.length < 8; 
  }, [form.password]);

  const passwordMismatch = useMemo(() => {
    if (!passwordTouched) return false;
    return (form.password ?? "") !== (form.confirmPassword ?? "");
  }, [passwordTouched, form.password, form.confirmPassword]);

  const canSubmit = useMemo(() => {
    if (!isAdmin) return false;
    if (!master?.masterId) return false;

    const baseOk =
      form.name.trim() &&
      form.surname.trim() &&
      form.nickName.trim() &&
      form.dateOfBirth &&
      form.email.trim();

    if (!baseOk) return false;
    if (passwordTooShort) return false;
    if (passwordMismatch) return false;

    return true;
  }, [form, isAdmin, master, passwordTooShort, passwordMismatch]);

  const onChange = (e) => {
    const { name, value } = e.target;
    setForm((f) => ({ ...f, [name]: value }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    e.stopPropagation();
    setValidated(true);

    if (!isAdmin) {
      showToast("Accesso negato: solo Admin", "danger");
      return;
    }

    if (!canSubmit) {
      if (passwordTooShort) {
        showToast("La password deve avere almeno 8 caratteri", "danger");
        return;
      }
      if (passwordMismatch) {
        showToast("Password e conferma password non coincidono", "danger");
        return;
      }
      showToast("Compila tutti i campi obbligatori", "danger");
      return;
    }

    setSaving(true);
    try {
      const payload = {
        name: form.name.trim(),
        surname: form.surname.trim(),
        nickName: form.nickName.trim(),
        dateOfBirth: form.dateOfBirth,
        avatarImgUrl: form.avatarImgUrl?.trim() ? form.avatarImgUrl.trim() : null,
        email: form.email.trim(),
        bioMaster: form.bioMaster?.trim() ? form.bioMaster.trim() : null,
        ...(form.password?.trim()
          ? { password: form.password.trim() }
          : {}),
      };

      await MastersApi.update(master.masterId, payload);

      // Dopo il salvataggio: l'upload imposta il nuovo URL della foto (il payload porta ancora quello attuale)
      const photoProblem = await uploadAfterSave(avatarFile, master.masterId, MastersApi.uploadAvatar);

      if (photoProblem) {
        showToast(`Master aggiornato, ma la foto non è stata caricata (${photoProblem}).`, "warning");
      } else {
        showToast("Master aggiornato ✅", "success");
      }
      closeAndReset();
      onUpdated?.();
    } catch (err) {
      const msg =
        err?.response?.data?.message ||
        err?.response?.data?.Message ||
        "Aggiornamento master fallito.";
      showToast(msg, "danger");
    } finally {
      setSaving(false);
    }
  };

  if (!isAdmin) {
    return (
      <Modal show={show} onHide={closeAndReset} centered animation>
        <Modal.Header closeButton>
          <Modal.Title>Accesso negato</Modal.Title>
        </Modal.Header>
        <Modal.Body className="">Solo gli Admin possono modificare i master.</Modal.Body>
      </Modal>
    );
  }

  return (
    <Modal
      show={show}
      onHide={closeAndReset}
      centered
      animation
      backdrop="static"
      size="lg"
      contentClassName="border-0 shadow"
    >
      <Modal.Header closeButton className="border-0 pb-0 text-white">
        <Modal.Title className="fw-semibold"> Modifica Master</Modal.Title>
      </Modal.Header>

      <Modal.Body className="pt-2 text-white">
        {!master ? (
          <div className="">Nessun master selezionato.</div>
        ) : loadingFull ? (
          <div className="text-center py-5">
            <Spinner animation="border" />
            <div className=" small mt-2">Caricamento dati…</div>
          </div>
        ) : (
          <Form noValidate validated={validated} onSubmit={handleSubmit}>
            <Row className="g-3">
              <Col xs={12} md={6}>
                <Form.Group>
                  <Form.Label>Nome *</Form.Label>
                  <Form.Control
                    name="name"
                    value={form.name}
                    onChange={onChange}
                    required
                  />
                </Form.Group>
              </Col>

              <Col xs={12} md={6}>
                <Form.Group>
                  <Form.Label>Cognome *</Form.Label>
                  <Form.Control
                    name="surname"
                    value={form.surname}
                    onChange={onChange}
                    required
                  />
                </Form.Group>
              </Col>

              <Col xs={12} md={6}>
                <Form.Group>
                  <Form.Label>NickName *</Form.Label>
                  <Form.Control
                    name="nickName"
                    value={form.nickName}
                    onChange={onChange}
                    required
                  />
                </Form.Group>
              </Col>

              <Col xs={12} md={6}>
                <Form.Group>
                  <Form.Label>Data di nascita *</Form.Label>
                  <Form.Control
                    type="date"
                    name="dateOfBirth"
                    value={form.dateOfBirth}
                    onChange={onChange}
                    required
                  />
                </Form.Group>
              </Col>

              <Col xs={12}>
                <Form.Group>
                  <Form.Label>Email *</Form.Label>
                  <Form.Control
                    type="email"
                    name="email"
                    value={form.email}
                    onChange={onChange}
                    required
                  />
                </Form.Group>
              </Col>
              
              <Col xs={12}>
                <ImageUploadField
                  label="Foto profilo"
                  shape="circle"
                  maxDimension={512}
                  file={avatarFile}
                  currentUrl={form.avatarImgUrl}
                  onChange={setAvatarFile}
                  disabled={saving}
                />
              </Col>

              <Col xs={12}>
                <Form.Group>
                  <Form.Label>Bio</Form.Label>
                  <Form.Control
                    as="textarea"
                    rows={3}
                    name="bioMaster"
                    value={form.bioMaster}
                    onChange={onChange}
                    placeholder="Descrizione breve..."
                  />
                </Form.Group>
              </Col>            
            </Row>

            <div className="d-flex flex-column flex-sm-row gap-2 justify-content-end mt-4">
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
                  "Salva modifiche"
                )}
              </Button>
            </div>
          </Form>
        )}
      </Modal.Body>
    </Modal>
  );
}
