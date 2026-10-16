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

const initial = {
  sessionTitle: "",
  sessionDescription: "",
  scheduledAt: "",
  duration: "",
  numbOfPlayer: 4,
  masterId: "",
  gameId: "",
  genreId: "",
  sessionLink: "",
};

export default function CreateSessionModal({ show, handleClose, onCreated }) {
  const { showToast } = useContext(ToastContext);
  const { user } = useContext(AuthContext);

  const [form, setForm] = useState(initial);
  const [validated, setValidated] = useState(false);
  const [saving, setSaving] = useState(false);
  const [coverFile, setCoverFile] = useState(null);

  const [loadingRefs, setLoadingRefs] = useState(false);
  const [games, setGames] = useState([]);
  const [genres, setGenres] = useState([]);
  const [masters, setMasters] = useState([]);
  const [meMaster, setMeMaster] = useState(null);

  const isAdmin = user?.role === "Admin";
  const isMaster = user?.role === "Master";
  const canCreate = isAdmin || isMaster;

  const closeAndReset = () => {
    setForm(initial);
    setValidated(false);
    setSaving(false);
    setCoverFile(null);
    setMeMaster(null);
    handleClose();
  };

  useEffect(() => {
    if (!show || !canCreate) return;

    const loadRefs = async () => {
      setLoadingRefs(true);
      try {
        // giochi + generi per tutti (admin/master)
        const [gRes, geRes] = await Promise.all([
          GamesApi.getAll(),
          GenresApi.getAll(),
        ]);

        setGames(gRes.data || []);
        setGenres(geRes.data || []);

        // admin -> carica lista master
        if (isAdmin) {
          const mRes = await MastersApi.getAll();
          setMasters(mRes.data || []);
        }

        // master -> ricava masterId automaticamente
        if (isMaster) {
          const meRes = await MastersApi.getMe();
          setMeMaster(meRes.data);
          setForm((f) => ({ ...f, masterId: meRes.data.masterId }));
        }
      } catch (err) {
        showToast("Errore caricamento dati (giochi/generi/master)", "danger");
      } finally {
        setLoadingRefs(false);
      }
    };

    loadRefs();
  }, [show, canCreate, isAdmin, isMaster]);

  const canSubmit = useMemo(() => {
    return (
      canCreate &&
      form.sessionTitle.trim() &&
      form.sessionDescription.trim() &&
      form.scheduledAt &&
      form.duration.trim() &&
      Number(form.numbOfPlayer) > 0 &&
      form.masterId &&
      form.gameId &&
      form.genreId
    );
  }, [form, canCreate]);

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

    if (!canSubmit) {
      showToast("Compila tutti i campi obbligatori", "danger");
      return;
    }

    setSaving(true);
    try {
      const payload = {
        sessionTitle: form.sessionTitle.trim(),
        sessionDescription: form.sessionDescription.trim(),
        scheduledAt: form.scheduledAt,
        duration: form.duration.trim(),
        numbOfPlayer: Number(form.numbOfPlayer),
        masterId: form.masterId,
        gameId: form.gameId,
        genreId: form.genreId,
        sessionLink: form.sessionLink?.trim() ? form.sessionLink.trim() : null,
      };

      const res = await SessionsApi.create(payload);

      // La cover si carica subito dopo la creazione (serve l'id della nuova sessione).
      // Se il caricamento fallisce la sessione resta creata: la cover si può aggiungere in seguito.
      const newSessionId = res?.data?.sessionId ?? res?.data?.SessionId;
      const coverProblem = await uploadAfterSave(coverFile, newSessionId, SessionsApi.uploadCover);

      if (coverProblem) {
        showToast(
          `Sessione creata, ma la cover non è stata caricata (${coverProblem}). Puoi aggiungerla in seguito.`,
          "warning"
        );
      } else {
        showToast("Sessione creata con successo ✅", "success");
      }
      closeAndReset();
      onCreated?.();
    } catch (err) {
      const msg =
        err?.response?.data?.message ||
        err?.response?.data?.Message ||
        "Creazione sessione fallita. Controlla i dati.";
      showToast(msg, "danger");
    } finally {
      setSaving(false);
    }
  };

  if (!canCreate) {
    return (
      <Modal show={show} onHide={closeAndReset} centered animation>
        <Modal.Header closeButton>
          <Modal.Title>Accesso negato</Modal.Title>
        </Modal.Header>
        <Modal.Body className="">
          Solo Admin o Master possono creare sessioni.
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
        <Modal.Title className="fw-semibold">+ Crea Sessione</Modal.Title>
      </Modal.Header>

      <Modal.Body className="pt-2 text-white">
        <p className=" small mb-3">
          Inserisci i dettagli principali e seleziona gioco e genere.
        </p>

        {loadingRefs ? (
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
                  <Form.Text className="">
                    Verrà mostrato come pulsante nella pagina dettagli sessione.
                  </Form.Text>
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

              {/* MASTER ID: Admin sceglie, Master è automatico */}
              {isAdmin && (
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
              )}

              {isMaster && (
                <Col xs={12} md={4}>
                  <Form.Group>
                    <Form.Label>Master</Form.Label>
                    <Form.Control
                      value={
                        meMaster
                          ? `${meMaster.nickName} (${meMaster.email})`
                          : "Caricamento…"
                      }
                      disabled
                    />
                    <Form.Text className="">
                      MasterId impostato automaticamente.
                    </Form.Text>
                  </Form.Group>
                </Col>
              )}
            </Row>

            <div className="d-flex flex-column flex-sm-row gap-2 justify-content-end mt-4">
              <Button variant="outline-secondary" onClick={closeAndReset}>
                Annulla
              </Button>
              <Button type="submit" disabled={!canSubmit || saving}>
                {saving ? (
                  <>
                    <Spinner size="sm" className="me-2" />
                    Creazione…
                  </>
                ) : (
                  "Crea Sessione"
                )}
              </Button>
            </div>
          </Form>
        )}
      </Modal.Body>
    </Modal>
  );
}
