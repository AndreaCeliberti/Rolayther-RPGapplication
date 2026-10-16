import { useContext, useEffect, useMemo, useState } from "react";
import { Modal, Button, Form, Row, Col, Spinner } from "react-bootstrap";
import { ToastContext } from "../../context/ToastContext";
import { AuthContext } from "../../context/AuthContext";

import { SessionsApi } from "../../api/sessions.api";
import { GamesApi } from "../../api/games.api";
import { GenresApi } from "../../api/genres.api";
import { MastersApi } from "../../api/masters.api";
import ImageUploadField from "../common/ImageUploadField";
import { uploadAfterSave } from "../../utils/apiError";

const empty = {
  sessionTitle: "",
  sessionDescription: "",
  scheduledAt: "", // datetime-local string
  duration: "",
  numbOfPlayer: 4,
  sessionLink: "",
  coverImgUrl: "", // URL attuale della cover (si cambia caricando un file)
  masterId: "",
  gameId: "",
  genreId: "",
};

// ISO -> "YYYY-MM-DDTHH:mm" per datetime-local
function isoToLocalInput(iso) {
  if (!iso) return "";
  const d = new Date(iso);
  const pad = (n) => String(n).padStart(2, "0");
  const yyyy = d.getFullYear();
  const mm = pad(d.getMonth() + 1);
  const dd = pad(d.getDate());
  const hh = pad(d.getHours());
  const mi = pad(d.getMinutes());
  return `${yyyy}-${mm}-${dd}T${hh}:${mi}`;
}

export default function UpdateSessionModal({ show, handleClose, onUpdated, session }) {
  const { showToast } = useContext(ToastContext);
  const { user } = useContext(AuthContext);

  const isAdmin = user?.role === "Admin";

  const [form, setForm] = useState(empty);
  const [validated, setValidated] = useState(false);
  const [saving, setSaving] = useState(false);
  const [coverFile, setCoverFile] = useState(null);

  const [loadingRefs, setLoadingRefs] = useState(false);
  const [games, setGames] = useState([]);
  const [genres, setGenres] = useState([]);
  const [masters, setMasters] = useState([]);

  const closeAndReset = () => {
    setValidated(false);
    setSaving(false);
    setCoverFile(null);
    setForm(empty);
    handleClose();
  };

  // Pre-fill form quando apri modal e hai session
  useEffect(() => {
    if (!show) return;
    if (!session) return;

    setCoverFile(null);
    setForm({
      sessionTitle: session.sessionTitle ?? "",
      sessionDescription: session.sessionDescription ?? "",
      scheduledAt: isoToLocalInput(session.scheduledAt),
      duration: session.duration ?? "",
      numbOfPlayer: session.numbOfPlayer ?? 4,
      sessionLink: session.sessionLink ?? "",
      coverImgUrl: session.coverImgUrl ?? "",
      masterId: session.masterId ?? "",
      gameId: session.gameId ?? "",
      genreId: session.genreId ?? "",
    });
  }, [show, session]);

  // Load references (masters/games/genres)
  useEffect(() => {
    if (!show || !isAdmin) return;

    const loadRefs = async () => {
      setLoadingRefs(true);
      try {
        const [gRes, geRes, mRes] = await Promise.all([
          GamesApi.getAll(),
          GenresApi.getAll(),
          MastersApi.getAll(),
        ]);
        setGames(gRes.data || []);
        setGenres(geRes.data || []);
        setMasters(mRes.data || []);
      } catch {
        showToast("Errore caricamento dati (giochi/generi/masters)", "danger");
      } finally {
        setLoadingRefs(false);
      }
    };

    loadRefs();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [show, isAdmin]);

  const canSubmit = useMemo(() => {
    if (!isAdmin) return false;
    if (!session?.sessionId) return false;

    return (
      form.sessionTitle.trim() &&
      form.sessionDescription.trim() &&
      form.scheduledAt &&
      form.duration.trim() &&
      Number(form.numbOfPlayer) > 0 &&
      form.masterId &&
      form.gameId &&
      form.genreId
    );
  }, [form, isAdmin, session]);

  const onChange = (e) => {
    const { name, value } = e.target;

    if (name === "numbOfPlayer") {
      setForm((f) => ({ ...f, [name]: value === "" ? "" : Number(value) }));
      return;
    }

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
      // datetime-local -> ISO (evita problemi timezone)
      const scheduledIso = new Date(form.scheduledAt).toISOString();

      const payload = {
        sessionTitle: form.sessionTitle.trim(),
        sessionDescription: form.sessionDescription.trim(),
        scheduledAt: scheduledIso,
        duration: form.duration.trim(),
        numbOfPlayer: Number(form.numbOfPlayer),
        sessionLink: form.sessionLink?.trim() ? form.sessionLink.trim() : null,
        // URL della cover attuale: va rimandato, altrimenti l'aggiornamento la cancellerebbe.
        // Se è stato scelto un nuovo file lo si carica subito dopo e sostituisce questo valore.
        coverImgUrl: form.coverImgUrl?.trim() ? form.coverImgUrl.trim() : null,
        masterId: form.masterId,
        gameId: form.gameId,
        genreId: form.genreId,
      };

      await SessionsApi.update(session.sessionId, payload);

      // Dopo il salvataggio, così il nuovo URL della cover non viene sovrascritto da quello vecchio del payload
      const coverProblem = await uploadAfterSave(coverFile, session.sessionId, SessionsApi.uploadCover);

      if (coverProblem) {
        showToast(`Sessione aggiornata, ma la cover non è stata caricata (${coverProblem}).`, "warning");
      } else {
        showToast("Sessione aggiornata ✅", "success");
      }
      closeAndReset();
      onUpdated?.();
    } catch (err) {
      const msg =
        err?.response?.data?.message ||
        err?.response?.data?.Message ||
        "Aggiornamento sessione fallito.";
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
        <Modal.Body className="">Solo gli Admin possono modificare sessioni.</Modal.Body>
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
        <Modal.Title className="fw-semibold"> Modifica Sessione</Modal.Title>
      </Modal.Header>

      <Modal.Body className="pt-2 text-white">
        {!session ? (
          <div className="">Nessuna sessione selezionata.</div>
        ) : loadingRefs ? (
          <div className="text-center py-5">
            <Spinner animation="border" />
            <div className=" small mt-2">Caricamento dati…</div>
          </div>
        ) : (
          <Form noValidate validated={validated} onSubmit={handleSubmit}>
            <Row className="g-3">
              <Col xs={12}>
                <Form.Group>
                  <Form.Label>Titolo sessione *</Form.Label>
                  <Form.Control
                    name="sessionTitle"
                    value={form.sessionTitle}
                    onChange={onChange}
                    required
                  />
                </Form.Group>
              </Col>

              <Col xs={12}>
                <Form.Group>
                  <Form.Label>Descrizione *</Form.Label>
                  <Form.Control
                    as="textarea"
                    rows={3}
                    name="sessionDescription"
                    value={form.sessionDescription}
                    onChange={onChange}
                    required
                  />
                </Form.Group>
              </Col>

              <Col xs={12}>
                <Form.Group>
                  <Form.Label>Link alla piattaforma di gioco</Form.Label>
                  <Form.Control
                    name="sessionLink"
                    value={form.sessionLink}
                    onChange={onChange}
                    placeholder="https://discord.gg/... "
                  />
                </Form.Group>
              </Col>

              <Col xs={12} md={6}>
                <Form.Group>
                  <Form.Label>Data & Ora *</Form.Label>
                  <Form.Control
                    type="datetime-local"
                    name="scheduledAt"
                    value={form.scheduledAt}
                    onChange={onChange}
                    required
                  />
                </Form.Group>
              </Col>

              <Col xs={12} md={3}>
                <Form.Group>
                  <Form.Label>Durata *</Form.Label>
                  <Form.Control
                    name="duration"
                    value={form.duration}
                    onChange={onChange}
                    required
                    placeholder="Es. 2h / 3h30"
                  />
                </Form.Group>
              </Col>

              <Col xs={12} md={3}>
                <Form.Group>
                  <Form.Label>N° Player *</Form.Label>
                  <Form.Control
                    type="number"
                    min={1}
                    name="numbOfPlayer"
                    value={form.numbOfPlayer}
                    onChange={onChange}
                    required
                  />
                </Form.Group>
              </Col>

              <Col xs={12}>
                <ImageUploadField
                  label="Cover della sessione"
                  maxDimension={1600}
                  file={coverFile}
                  currentUrl={form.coverImgUrl}
                  onChange={setCoverFile}
                  disabled={saving}
                />
              </Col>

              <Col xs={12} md={4}>
                <Form.Group>
                  <Form.Label>Gioco *</Form.Label>
                  <Form.Select
                    name="gameId"
                    value={form.gameId}
                    onChange={onChange}
                    required
                  >
                    <option value="">Seleziona gioco…</option>
                    {games.map((g) => (
                      <option key={g.gameId} value={g.gameId}>
                        {g.title}
                      </option>
                    ))}
                  </Form.Select>
                </Form.Group>
              </Col>

              <Col xs={12} md={4}>
                <Form.Group>
                  <Form.Label>Genere *</Form.Label>
                  <Form.Select
                    name="genreId"
                    value={form.genreId}
                    onChange={onChange}
                    required
                  >
                    <option value="">Seleziona genere…</option>
                    {genres.map((ge) => (
                      <option key={ge.genreId} value={ge.genreId}>
                        {ge.name}
                      </option>
                    ))}
                  </Form.Select>
                </Form.Group>
              </Col>

              <Col xs={12} md={4}>
                <Form.Group>
                  <Form.Label>Master *</Form.Label>
                  <Form.Select
                    name="masterId"
                    value={form.masterId}
                    onChange={onChange}
                    required
                  >
                    <option value="">Seleziona master…</option>
                    {masters.map((m) => (
                      <option key={m.masterId} value={m.masterId}>
                        {m.nickName || `${m.name} ${m.surname}`}
                      </option>
                    ))}
                  </Form.Select>
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
