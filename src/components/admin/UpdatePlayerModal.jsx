import { useContext, useEffect, useMemo, useState } from "react";
import { Modal, Button, Form, Row, Col, Spinner } from "react-bootstrap";
import { ToastContext } from "../../context/ToastContext";
import { AuthContext } from "../../context/AuthContext";
import { PlayersApi } from "../../api/players.api";
import ImageUploadField from "../common/ImageUploadField";
import { uploadAfterSave } from "../../utils/apiError";

const empty = {
  name: "",
  surname: "",
  nickName: "",
  dateOfBirth: "", // yyyy-mm-dd
  avatarImgUrl: "", // URL attuale della foto (si cambia caricando un file)
  email: "",
  bioPlayer: "",
};

function dateOnlyToInput(value) {
  // value può arrivare come "2025-01-10" oppure ISO
  if (!value) return "";
  if (typeof value === "string" && /^\d{4}-\d{2}-\d{2}$/.test(value)) return value;
  const d = new Date(value);
  if (Number.isNaN(d.getTime())) return "";
  const pad = (n) => String(n).padStart(2, "0");
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
}

export default function UpdatePlayerModal({
  show,
  handleClose,
  onUpdated,
  player, // oggetto selezionato dalla tabella
}) {
  const { showToast } = useContext(ToastContext);
  const { user } = useContext(AuthContext);
  const isAdmin = user?.role === "Admin";

  const [form, setForm] = useState(empty);
  const [validated, setValidated] = useState(false);
  const [saving, setSaving] = useState(false);
  const [avatarFile, setAvatarFile] = useState(null);

  // opzionale: se vuoi ricaricare dal backend l’oggetto completo quando apri
  const [loadingFull, setLoadingFull] = useState(false);

  const closeAndReset = () => {
    setValidated(false);
    setSaving(false);
    setLoadingFull(false);
    setAvatarFile(null);
    setForm(empty);
    handleClose();
  };

  // Prefill: usa il player passato dalla tabella, poi (opzionale) fetch completo
  useEffect(() => {
    if (!show) return;
    if (!player) return;

    setAvatarFile(null);
    setForm({
      name: player.name ?? "",
      surname: player.surname ?? "",
      nickName: player.nickName ?? "",
      dateOfBirth: dateOnlyToInput(player.dateOfBirth),
      avatarImgUrl: player.avatarImgUrl ?? "",
      email: player.email ?? "",
      bioPlayer: player.bioPlayer ?? "",
    });

    // Se vuoi essere sicuro di avere i dati aggiornati:
    const fetchFull = async () => {
      setLoadingFull(true);
      try {
        const res = await PlayersApi.getById(player.playerId);
        const p = res.data;
        setForm({
          name: p.name ?? "",
          surname: p.surname ?? "",
          nickName: p.nickName ?? "",
          dateOfBirth: dateOnlyToInput(p.dateOfBirth),
          avatarImgUrl: p.avatarImgUrl ?? "",
          email: p.email ?? "",
          bioPlayer: p.bioPlayer ?? "",
        });
      } catch {
        // non blocco il modal: resto coi dati già presenti
      } finally {
        setLoadingFull(false);
      }
    };

    fetchFull();
  }, [show, player]);

  const canSubmit = useMemo(() => {
    if (!isAdmin) return false;
    if (!player?.playerId) return false;

    return (
      form.name.trim() &&
      form.surname.trim() &&
      form.nickName.trim() &&
      form.dateOfBirth &&
      form.email.trim()
    );
  }, [form, isAdmin, player]);

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
      showToast("Compila tutti i campi obbligatori", "danger");
      return;
    }

    setSaving(true);
    try {
      // Il tuo DTO usa DateOnly: passiamo "YYYY-MM-DD"
      const payload = {
        name: form.name.trim(),
        surname: form.surname.trim(),
        nickName: form.nickName.trim(),
        dateOfBirth: form.dateOfBirth, // "YYYY-MM-DD"
        avatarImgUrl: form.avatarImgUrl?.trim() ? form.avatarImgUrl.trim() : null,
        email: form.email.trim(),
        bioPlayer: form.bioPlayer?.trim() ? form.bioPlayer.trim() : null,
      };

      await PlayersApi.update(player.playerId, payload);

      // Dopo il salvataggio: l'upload imposta il nuovo URL della foto (il payload porta ancora quello attuale)
      const photoProblem = await uploadAfterSave(avatarFile, player.playerId, PlayersApi.uploadAvatar);

      if (photoProblem) {
        showToast(`Player aggiornato, ma la foto non è stata caricata (${photoProblem}).`, "warning");
      } else {
        showToast("Player aggiornato ✅", "success");
      }
      closeAndReset();
      onUpdated?.();
    } catch (err) {
      const msg =
        err?.response?.data?.message ||
        err?.response?.data?.Message ||
        "Aggiornamento player fallito.";
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
        <Modal.Body className="">
          Solo gli Admin possono modificare i player.
        </Modal.Body>
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
        <Modal.Title className="fw-semibold"> Modifica Player</Modal.Title>
      </Modal.Header>

      <Modal.Body className="pt-2 text-white">
        {!player ? (
          <div className="">Nessun player selezionato.</div>
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
                    name="bioPlayer"
                    value={form.bioPlayer}
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
