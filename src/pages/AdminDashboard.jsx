import { useContext, useEffect, useMemo, useState } from "react";
import {
  Container,
  Tabs,
  Tab,
  Card,
  Table,
  Button,
  Spinner,
  Stack,
} from "react-bootstrap";

import { AuthContext } from "../context/AuthContext";
import { ToastContext } from "../context/ToastContext";

import PageHeader from "../components/common/PageHeader";
import EmptyState from "../components/common/EmptyState";
import { Chip, RoleBadge } from "../components/common/Badges";
import ImageUploadButton from "../components/common/ImageUploadButton";
import ImageThumb from "../components/common/ImageThumb";

import { SessionsApi } from "../api/sessions.api";
import { PlayersApi } from "../api/players.api";
import { MastersApi } from "../api/masters.api";
import { GamesApi } from "../api/games.api";
import { GenresApi } from "../api/genres.api";
import { PlatformsApi } from "../api/platforms.api";

import CreateGameModal from "../components/admin/CreateGameModal";
import CreateGenreModal from "../components/admin/CreateGenreModal";
import CreatePlatformModal from "../components/admin/CreatePlatformModal";
import CreateMasterModal from "../components/admin/CreateMasterModal";
import CreatePlayerModal from "../components/admin/CreatePlayerModal";
import UpdateSessionModal from "../components/admin/UpdateSessionModal";
import UpdatePlayerModal from "../components/admin/UpdatePlayerModal";
import UpdateMasterModal from "../components/admin/UpdateMasterModal";
import ConfirmDeleteModal from "../components/common/ConfirmDeleteModal";
import CreateSessionModal from "../components/admin/CreateSessionModal";
import MasterDecisionModal from "../components/admin/MasterDecisionModal";


export default function AdminDashboard() {
  const { user } = useContext(AuthContext);
  const { showToast } = useContext(ToastContext);
  
  const isAdmin = user?.role === "Admin";

  const [activeKey, setActiveKey] = useState("sessions");

  // modali create
  const [showCreateGame, setShowCreateGame] = useState(false);
  const [showCreateGenre, setShowCreateGenre] = useState(false);
  const [showCreatePlatform, setShowCreatePlatform] = useState(false);
  const [showCreateSession, setShowCreateSession] = useState(false);
  const [showCreatePlayer, setShowCreatePlayer] = useState(false);
  const [showCreateMaster, setShowCreateMaster] = useState(false);

  // modali update
  const [showUpdateSession, setShowUpdateSession] = useState(false);
  const [selectedSession, setSelectedSession] = useState(null);
  const [showUpdatePlayer, setShowUpdatePlayer] = useState(false);
  const [selectedPlayer, setSelectedPlayer] = useState(null);
  const [showUpdateMaster, setShowUpdateMaster] = useState(false);
  const [selectedMaster, setSelectedMaster] = useState(null);

  // state dati
  const [loading, setLoading] = useState({
    sessions: true,
    players: true,
    masters: true,
    pendingMasters: true,
    games: true,
    genres: true,
    platforms: true,
  });

  const [pendingMasters, setPendingMasters] = useState([]);
  const [approving, setApproving] = useState(null); // masterId in corso di approvazione/rifiuto
  // finestra con cui si approva (messaggio facoltativo) o si rifiuta (motivo obbligatorio): il testo va nell'email al candidato
  const [decision, setDecision] = useState({ show: false, mode: "approve", master: null });

  const [sessions, setSessions] = useState([]);
  const [players, setPlayers] = useState([]);
  const [masters, setMasters] = useState([]);
  const [games, setGames] = useState([]);
  const [genres, setGenres] = useState([]);
  const [platforms, setPlatforms] = useState([]);

  const setLoadingKey = (key, value) =>
    setLoading((l) => ({ ...l, [key]: value }));

  const parseMsg = (err, fallback) =>
    err?.response?.data?.message ||
    err?.response?.data?.Message ||
    fallback;

  // LOADERS
  const loadSessions = async () => {
    setLoadingKey("sessions", true);
    try {
      const res = await SessionsApi.getAll();
      setSessions(res.data || []);
    } catch (err) {
      showToast(parseMsg(err, "Errore caricamento sessioni"), "danger");
      setSessions([]);
    } finally {
      setLoadingKey("sessions", false);
    }
  };

  const loadPlayers = async () => {
    setLoadingKey("players", true);
    try {
      const res = await PlayersApi.getAll();
      setPlayers(res.data || []);
    } catch (err) {
      showToast(parseMsg(err, "Errore caricamento players"), "danger");
      setPlayers([]);
    } finally {
      setLoadingKey("players", false);
    }
  };

  const loadMasters = async () => {
    setLoadingKey("masters", true);
    try {
      const res = await MastersApi.getAll();
      setMasters(res.data || []);
    } catch (err) {
      showToast(parseMsg(err, "Errore caricamento masters"), "danger");
      setMasters([]);
    } finally {
      setLoadingKey("masters", false);
    }
  };

  const loadPendingMasters = async () => {
    setLoadingKey("pendingMasters", true);
    try {
      const res = await MastersApi.getPending();
      setPendingMasters(res.data || []);
    } catch (err) {
      showToast(parseMsg(err, "Errore caricamento richieste"), "danger");
      setPendingMasters([]);
    } finally {
      setLoadingKey("pendingMasters", false);
    }
  };

  const openDecision = (mode, master) => setDecision({ show: true, mode, master });
  const closeDecision = () => setDecision((d) => ({ ...d, show: false }));

  // Conferma dalla finestra: text = messaggio di benvenuto (approvazione, può essere vuoto) o motivo (rifiuto)
  const handleDecisionConfirm = async (text) => {
    const { mode, master } = decision;
    if (!master) return;

    setApproving(master.masterId);
    try {
      const res =
        mode === "approve"
          ? await MastersApi.approve(master.masterId, text)
          : await MastersApi.reject(master.masterId, text);

      // emailSent false = decisione salvata, ma l'email al candidato NON è partita: l'Admin deve saperlo
      const emailSent = res?.data?.emailSent !== false;
      const done = mode === "approve" ? `${master.nickName} approvato come Master` : `Richiesta di ${master.nickName} rifiutata`;

      if (emailSent) {
        showToast(`${done}. Email inviata a ${master.email}.`, "success", 5000);
      } else {
        showToast(`${done}, ma l'email NON è partita: avvisa ${master.nickName} in un altro modo.`, "warning", 9000);
      }

      closeDecision();
      await Promise.all(mode === "approve" ? [loadPendingMasters(), loadMasters()] : [loadPendingMasters()]);
    } catch (err) {
      // la finestra resta aperta: il testo scritto non si perde e si può riprovare
      showToast(parseMsg(err, mode === "approve" ? "Approvazione fallita" : "Rifiuto fallito"), "danger");
    } finally {
      setApproving(null);
    }
  };

  const loadGames = async () => {
    setLoadingKey("games", true);
    try {
      const res = await GamesApi.getAll();
      setGames(res.data || []);
    } catch (err) {
      showToast(parseMsg(err, "Errore caricamento games"), "danger");
      setGames([]);
    } finally {
      setLoadingKey("games", false);
    }
  };

  const loadGenres = async () => {
    setLoadingKey("genres", true);
    try {
      const res = await GenresApi.getAll();
      setGenres(res.data || []);
    } catch (err) {
      showToast(parseMsg(err, "Errore caricamento genres"), "danger");
      setGenres([]);
    } finally {
      setLoadingKey("genres", false);
    }
  };

  const loadPlatforms = async () => {
    setLoadingKey("platforms", true);
    try {
      const res = await PlatformsApi.getAll();
      setPlatforms(res.data || []);
    } catch (err) {
      showToast(parseMsg(err, "Errore caricamento platforms"), "danger");
      setPlatforms([]);
    } finally {
      setLoadingKey("platforms", false);
    }
    
  };

  // Carica una nuova immagine per un elemento del catalogo e aggiorna solo la sua riga
  // (la risposta del server contiene il nuovo URL sotto la chiave indicata da urlKey)
  const changeCatalogImage = async (uploadFn, id, file, { setList, idKey, urlKey }) => {
    const res = await uploadFn(id, file);
    const url = res?.data?.[urlKey];
    if (url) {
      setList((list) => list.map((x) => (x[idKey] === id ? { ...x, [urlKey]: url } : x)));
    }
  };

  // primo load
  useEffect(() => {
    if (!isAdmin) return;
    loadSessions();
    loadPlayers();
    loadMasters();
    loadPendingMasters();
    loadGames();
    loadGenres();
    loadPlatforms();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isAdmin]);

  // delete modal 
  const [deleteModal, setDeleteModal] = useState({
    show: false,
    type: null, // "session" | "player" | "master" | "game" | "genre" | "platform"
    item: null,
  });
  const [deleting, setDeleting] = useState(false);

  const openDelete = (type, item) => setDeleteModal({ show: true, type, item });
  const closeDelete = () => setDeleteModal({ show: false, type: null, item: null });

  const doDelete = async () => {
  if (!deleteModal.item) return;

  setDeleting(true);
  try {
    const { type, item } = deleteModal;

    if (type === "session") await SessionsApi.delete(item.sessionId);
    if (type === "player") await PlayersApi.delete(item.playerId);
    if (type === "master") await MastersApi.delete(item.masterId);
    if (type === "game") await GamesApi.delete(item.gameId);
    if (type === "genre") await GenresApi.delete(item.genreId);
    if (type === "platform") await PlatformsApi.delete(item.platformId);

    showToast("Eliminato con successo ", "success");
    closeDelete();

    // refresh tab corretta
    if (type === "session") loadSessions();
    if (type === "player") loadPlayers();
    if (type === "master") loadMasters();
    if (type === "game") loadGames();
    if (type === "genre") loadGenres();
    if (type === "platform") loadPlatforms();
  } catch (err) {
    showToast(parseMsg(err, "Eliminazione fallita"), "danger");
  } finally {
    setDeleting(false);
  }
};


  // UI helpers
  const TableShell = ({ loading, emptyTitle, emptyHint, children }) => {
    if (loading) {
      return (
        <div className="text-center py-5">
          <Spinner animation="border" />
        </div>
      );
    }
    if (!children) return null;
    return children;
  };

  const HeaderActions = useMemo(() => {
    return (
      <Stack direction="horizontal" gap={2} className="flex-wrap">
        <RoleBadge role="Admin" />
      </Stack>
    );
  }, []);
  const deleteTitle = useMemo(() => {
  switch (deleteModal.type) {
    case "session":
      return "Eliminare la sessione?";
    case "player":
      return "Eliminare il player?";
    case "master":
      return "Eliminare il master?";
    case "game":
      return "Eliminare il gioco?";
    case "genre":
      return "Eliminare il genere?";
    case "platform":
      return "Eliminare la piattaforma?";
    default:
      return "Conferma eliminazione";
  }
}, [deleteModal.type]);

const deleteBody = useMemo(() => {
  const i = deleteModal.item;
  if (!i) return null;

  if (deleteModal.type === "session")
    return `Stai per eliminare "${i.sessionTitle}". L'operazione non è reversibile.`;
  if (deleteModal.type === "player")
    return `Stai per eliminare "${i.nickName}" (${i.email}). L'operazione non è reversibile.`;
  if (deleteModal.type === "master")
    return `Stai per eliminare "${i.nickName}" (${i.email}). L'operazione non è reversibile.`;
  if (deleteModal.type === "game")
    return `Stai per eliminare "${i.title}". L'operazione non è reversibile.`;
  if (deleteModal.type === "genre")
    return `Stai per eliminare "${i.name}". L'operazione non è reversibile.`;
  if (deleteModal.type === "platform")
    return `Stai per eliminare "${i.name}". L'operazione non è reversibile.`;

  return "L'operazione non è reversibile.";
}, [deleteModal.type, deleteModal.item]);

  if (!isAdmin) {
    return (
      <>
        <PageHeader  title="Admin Dashboard" subtitle="Area riservata" />
        <Container fluid="md" className="pb-4 px-3 px-md-0">
          <EmptyState
            icon="⛔"
            title="Accesso negato"
            hint="Solo gli Admin possono accedere a questa pagina."
          />
        </Container>
      </>
    );
  }

  return (
    <>
      <PageHeader
        title="Admin Dashboard"
        subtitle="Gestione completa dei contenuti (CRUD)."
        actions={HeaderActions}
      />

      <Container fluid="lg" className="pb-4 px-3 px-md-0">
        <Card  className="border-0 shadow-sm">
          <Card.Body>
            <Tabs
              activeKey={activeKey}
              onSelect={(k) => setActiveKey(k)}
              className="mb-3"
            >
              {/* SESSIONS */}
              <Tab  eventKey="sessions" title={`Session (${sessions.length})`}>
                <div className="d-flex justify-content-between align-items-center mb-3 gap-2 flex-wrap">
                  <div className=" small">
                    Gestisci tutte le sessioni (crea/modifica/cancella).
                  </div>
                  <div className="d-flex gap-2 flex-wrap">
                    <Button
                      variant="outline-secondary"
                      size="sm"
                      onClick={loadSessions}
                    >
                      ↻ Aggiorna
                    </Button>
                    <Button
                      size="sm"
                       onClick={() => setShowCreateSession(true)}
                    >
                      + Crea Sessione
                    </Button>
                  </div>
                </div>

                {loading.sessions ? (
                  <div className="text-center py-5">
                    <Spinner animation="border" />
                  </div>
                ) : sessions.length === 0 ? (
                  <EmptyState
                    title="Nessuna sessione"
                    hint="Non ci sono sessioni nel sistema."
                    icon="📅"
                  />
                ) : (
                  <div className="table-responsive">
                    <Table style={{ color: "var(--muted)" }} hover variant="dark" className="align-middle table-dark">
                      <thead className="table-dark">
                        <tr style={{ color: "var(--muted)" }}>
                          <th style={{ color: "var(--muted)" }}>Titolo</th>
                          <th>Data</th>
                          <th>Durata</th>
                          <th>Players</th>
                          <th className="text-end">Azioni</th>
                        </tr>
                      </thead>
                      <tbody >
                        {sessions.map((s) => (
                          <tr key={s.sessionId}>
                            <td className=" fw-semibold">{s.sessionTitle}</td>
                            <td>
                              {s.scheduledAt
                                ? new Date(s.scheduledAt).toLocaleString()
                                : "—"}
                            </td>
                            <td>{s.duration}</td>
                            <td>
                              <Chip>
                                {(s.players?.length ?? 0)}/{s.numbOfPlayer}
                              </Chip>
                            </td>
                            <td className="text-end">
                              <div className="d-inline-flex gap-2">
                               <Button
                                size="sm"
                                variant="outline-primary"
                                onClick={() => {
                                setSelectedSession(s);
                                setShowUpdateSession(true);
                                }}
                                >   
                                Modifica
                                </Button>
                                <Button
                                  size="sm"
                                  variant="outline-danger"
                                  onClick={() => openDelete("session", s)}
                                >
                                  Cancella
                                </Button>

                              </div>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </Table>
                  </div>
                )}

              
              </Tab>

              {/* PLAYERS */}
              <Tab eventKey="players" title={`Players (${players.length})`}>
                <div className="d-flex justify-content-between align-items-center mb-3 gap-2 flex-wrap">
                  <div className="text-muted small">
                    Gestione player (crea/modifica/cancella).
                  </div>
                  <div className="d-flex gap-2 flex-wrap">
                    <Button
                      variant="outline-secondary"
                      size="sm"
                      onClick={loadPlayers}
                    >
                      ↻ Aggiorna
                    </Button>
                    <Button size="sm" onClick={() => setShowCreatePlayer(true)}>
                      + Crea Player
                    </Button>
                  </div>
                </div>

                {loading.players ? (
                  <div className="text-center py-5">
                    <Spinner animation="border" />
                  </div>
                ) : players.length === 0 ? (
                  <EmptyState
                    title="Nessun player"
                    hint="Non ci sono player nel sistema."
                    icon="👤"
                  />
                ) : (
                  <div className="table-responsive">
                    <Table hover variant="dark" className="align-middle">
                      <thead>
                        <tr>
                          <th>Nick</th>
                          <th>Email</th>
                          <th>Nome</th>
                          <th className="text-end">Azioni</th>
                        </tr>
                      </thead>
                      <tbody>
                        {players.map((p) => (
                          <tr key={p.playerId}>
                            <td className="fw-semibold">{p.nickName}</td>
                            <td className="text-muted">{p.email}</td>
                            <td>
                              {p.name} {p.surname}
                            </td>
                            <td className="text-end">
                              <div className="d-inline-flex gap-2">
                                <Button
                                    size="sm"
                                    variant="outline-primary"
                                    onClick={() => {
                                    setSelectedPlayer(p);
                                    setShowUpdatePlayer(true);
                                    }}
                                >
                                  Modifica
                                </Button>

                                <Button
                                  size="sm"
                                  variant="outline-danger"
                                  onClick={() => openDelete("player", p)}
                                >
                                  Cancella
                                </Button>

                              </div>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </Table>
                  </div>
                )}

                {showCreatePlayer && (
                  <div className="text-muted small mt-3">
                    <Button onClick={() => setShowCreatePlayer(true)}>+ Crea</Button>
                  </div>
                )}
              </Tab>

              {/* MASTERS */}
              <Tab eventKey="masters" title={`Masters (${masters.length})`}>
                <div className="d-flex justify-content-between align-items-center mb-3 gap-2 flex-wrap">
                  <div className="text-muted small">
                    Gestione master (crea/modifica/cancella).
                  </div>
                  <div className="d-flex gap-2 flex-wrap">
                    <Button
                      variant="outline-secondary"
                      size="sm"
                      onClick={loadMasters}
                    >
                      ↻ Aggiorna
                    </Button>
                    <Button size="sm" onClick={() => setShowCreateMaster(true)}>
                      + Crea Master
                    </Button>
                  </div>
                </div>

                {loading.masters ? (
                  <div className="text-center py-5">
                    <Spinner animation="border" />
                  </div>
                ) : masters.length === 0 ? (
                  <EmptyState
                    title="Nessun master"
                    hint="Non ci sono master nel sistema."
                    icon="🧙"
                  />
                ) : (
                  <div className="table-responsive">
                    <Table hover variant="dark" className="align-middle">
                      <thead>
                        <tr>
                          <th>Nick</th>
                          <th>Email</th>
                          <th>Nome</th>
                          <th className="text-end">Azioni</th>
                        </tr>
                      </thead>
                      <tbody>
                        {masters.map((m) => (
                          <tr key={m.masterId}>
                            <td className="fw-semibold">{m.nickName}</td>
                            <td className="text-muted">{m.email}</td>
                            <td>
                              {m.name} {m.surname}
                            </td>
                            <td className="text-end">
                              <div className="d-inline-flex gap-2">
                                <Button
                                    size="sm"
                                    variant="outline-primary"
                                    onClick={() => {
                                     setSelectedMaster(m);
                                     setShowUpdateMaster(true);
                                      }}
                                >
                                  Modifica
                                </Button>
                                <Button
                                  size="sm"
                                  variant="outline-danger"
                                  onClick={() => openDelete("master", m)}
                                >
                                  Cancella
                                </Button>

                              </div>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </Table>
                  </div>
                )}
              </Tab>

              {/* PENDING MASTER REQUESTS */}
              <Tab
                eventKey="pendingMasters"
                title={`Richieste in attesa (${pendingMasters.length})`}
              >
                <div className="d-flex justify-content-between align-items-center mb-3 gap-2 flex-wrap">
                  <div className="text-muted small">
                    Richieste di registrazione come Master in attesa di approvazione.
                  </div>
                  <div className="d-flex gap-2 flex-wrap">
                    <Button
                      variant="outline-secondary"
                      size="sm"
                      onClick={loadPendingMasters}
                    >
                      ↻ Aggiorna
                    </Button>
                  </div>
                </div>

                {loading.pendingMasters ? (
                  <div className="text-center py-5">
                    <Spinner animation="border" />
                  </div>
                ) : pendingMasters.length === 0 ? (
                  <EmptyState
                    title="Nessuna richiesta in attesa"
                    hint="Le nuove registrazioni Master compariranno qui."
                    icon="⏳"
                  />
                ) : (
                  <div className="table-responsive">
                    <Table hover variant="dark" className="align-middle">
                      <thead>
                        <tr>
                          <th>Nick</th>
                          <th>Email</th>
                          <th>Nome</th>
                          <th className="text-end">Azioni</th>
                        </tr>
                      </thead>
                      <tbody>
                        {pendingMasters.map((m) => (
                          <tr key={m.masterId}>
                            <td className="fw-semibold">{m.nickName}</td>
                            <td className="text-muted">{m.email}</td>
                            <td>
                              {m.name} {m.surname}
                            </td>
                            <td className="text-end">
                              <div className="d-inline-flex gap-2">
                                <Button
                                  size="sm"
                                  variant="outline-success"
                                  disabled={approving === m.masterId}
                                  onClick={() => openDecision("approve", m)}
                                >
                                  Approva
                                </Button>
                                <Button
                                  size="sm"
                                  variant="outline-danger"
                                  disabled={approving === m.masterId}
                                  onClick={() => openDecision("reject", m)}
                                >
                                  Rifiuta
                                </Button>
                              </div>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </Table>
                  </div>
                )}
              </Tab>

              {/* GAMES */}
              <Tab eventKey="games" title={`Games (${games.length})`}>
                <div className="d-flex justify-content-between align-items-center mb-3 gap-2 flex-wrap">
                  <div className="text-muted small">
                    Gestione giochi (crea, cambia cover, cancella).
                  </div>
                  <div className="d-flex gap-2 flex-wrap">
                    <Button variant="outline-secondary" size="sm" onClick={loadGames}>
                      ↻ Aggiorna
                    </Button>
                    <Button size="sm" onClick={() => setShowCreateGame(true)}>
                      + Crea Gioco
                    </Button>
                  </div>
                </div>

                {loading.games ? (
                  <div className="text-center py-5">
                    <Spinner animation="border" />
                  </div>
                ) : games.length === 0 ? (
                  <EmptyState title="Nessun gioco" hint="Crea il primo gioco." icon="🎮" />
                ) : (
                  <div className="table-responsive">
                    <Table hover variant="dark" className="align-middle">
                      <thead>
                        <tr>
                          <th>Titolo</th>
                          <th className="text-end">Azioni</th>
                        </tr>
                      </thead>
                      <tbody>
                        {games.map((g) => (
                          <tr key={g.gameId}>
                            <td className="fw-semibold">
                              <div className="d-flex align-items-center gap-2">
                                <ImageThumb url={g.coverImageUrl} />
                                {g.title}
                              </div>
                            </td>
                            <td className="text-end">
                              <div className="d-flex gap-2 justify-content-end flex-wrap">
                                <ImageUploadButton
                                  label={g.coverImageUrl ? "Cambia cover" : "Aggiungi cover"}
                                  maxDimension={1600}
                                  onUpload={(file) =>
                                    changeCatalogImage(GamesApi.uploadCover, g.gameId, file, {
                                      setList: setGames,
                                      idKey: "gameId",
                                      urlKey: "coverImageUrl",
                                    })
                                  }
                                />
                                <Button
                                  size="sm"
                                  variant="outline-danger"
                                  onClick={() => openDelete("game", g)}
                                >
                                  Cancella
                                </Button>
                              </div>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </Table>
                  </div>
                )}
              </Tab>

              {/* GENRES */}
              <Tab eventKey="genres" title={`Genres (${genres.length})`}>
                <div className="d-flex justify-content-between align-items-center mb-3 gap-2 flex-wrap">
                  <div className="text-muted small">
                    Gestione generi (crea, cambia immagine, cancella).
                  </div>
                  <div className="d-flex gap-2 flex-wrap">
                    <Button
                      variant="outline-secondary"
                      size="sm"
                      onClick={loadGenres}
                    >
                      ↻ Aggiorna
                    </Button>
                    <Button size="sm" onClick={() => setShowCreateGenre(true)}>
                      + Crea Genere
                    </Button>
                  </div>
                </div>

                {loading.genres ? (
                  <div className="text-center py-5">
                    <Spinner animation="border" />
                  </div>
                ) : genres.length === 0 ? (
                  <EmptyState title="Nessun genere" hint="Crea il primo genere." icon="🏷️" />
                ) : (
                  <div className="table-responsive">
                    <Table hover variant="dark" className="align-middle">
                      <thead>
                        <tr>
                          <th>Nome</th>
                          <th className="text-end">Azioni</th>
                        </tr>
                      </thead>
                      <tbody>
                        {genres.map((g) => (
                          <tr key={g.genreId}>
                            <td className="fw-semibold">
                              <div className="d-flex align-items-center gap-2">
                                <ImageThumb url={g.imageUrl} />
                                {g.name}
                              </div>
                            </td>
                            <td className="text-end">
                              <div className="d-flex gap-2 justify-content-end flex-wrap">
                                <ImageUploadButton
                                  label={g.imageUrl ? "Cambia immagine" : "Aggiungi immagine"}
                                  maxDimension={1600}
                                  onUpload={(file) =>
                                    changeCatalogImage(GenresApi.uploadImage, g.genreId, file, {
                                      setList: setGenres,
                                      idKey: "genreId",
                                      urlKey: "imageUrl",
                                    })
                                  }
                                />
                                <Button
                                  size="sm"
                                  variant="outline-danger"
                                  onClick={() => openDelete("genre", g)}
                                >
                                  Cancella
                                </Button>
                              </div>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </Table>
                  </div>
                )}
              </Tab>

              {/* PLATFORMS */}
              <Tab eventKey="platforms" title={`Platforms (${platforms.length})`}>
                <div className="d-flex justify-content-between align-items-center mb-3 gap-2 flex-wrap">
                  <div className="text-muted small">
                    Gestione piattaforme (crea, cambia logo, cancella).
                  </div>
                  <div className="d-flex gap-2 flex-wrap">
                    <Button
                      variant="outline-secondary"
                      size="sm"
                      onClick={loadPlatforms}
                    >
                      ↻ Aggiorna
                    </Button>
                    <Button size="sm" onClick={() => setShowCreatePlatform(true)}>
                      + Crea Platform
                    </Button>
                  </div>
                </div>

                {loading.platforms ? (
                  <div className="text-center py-5">
                    <Spinner animation="border" />
                  </div>
                ) : platforms.length === 0 ? (
                  <EmptyState
                    title="Nessuna piattaforma"
                    hint="Crea la prima piattaforma."
                    icon="🧩"
                  />
                ) : (
                  <div className="table-responsive">
                    <Table hover variant="dark" className="align-middle">
                      <thead>
                        <tr>
                          <th>Nome</th>
                          <th className="text-end">Azioni</th>
                        </tr>
                      </thead>
                      <tbody>
                        {platforms.map((p) => (
                          <tr key={p.platformId}>
                            <td className="fw-semibold">
                              <div className="d-flex align-items-center gap-2">
                                <ImageThumb url={p.logoUrl} />
                                {p.name}
                              </div>
                            </td>
                            <td className="text-end">
                              <div className="d-flex gap-2 justify-content-end flex-wrap">
                                <ImageUploadButton
                                  label={p.logoUrl ? "Cambia logo" : "Aggiungi logo"}
                                  maxDimension={512}
                                  onUpload={(file) =>
                                    changeCatalogImage(PlatformsApi.uploadLogo, p.platformId, file, {
                                      setList: setPlatforms,
                                      idKey: "platformId",
                                      urlKey: "logoUrl",
                                    })
                                  }
                                />
                                <Button
                                  size="sm"
                                  variant="outline-danger"
                                  onClick={() => openDelete("platform", p)}
                                >
                                  Cancella
                                </Button>
                              </div>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </Table>
                  </div>
                )}
              </Tab>
            </Tabs>
          </Card.Body>
        </Card>

        {/* MODALS */}
        <CreateGameModal
          show={showCreateGame}
          handleClose={() => setShowCreateGame(false)}
          onCreated={loadGames}
        />

        <CreateGenreModal
          show={showCreateGenre}
          handleClose={() => setShowCreateGenre(false)}
          onCreated={loadGenres}
        />

        <CreatePlatformModal
          show={showCreatePlatform}
          handleClose={() => setShowCreatePlatform(false)}
          onCreated={loadPlatforms}
        />
        <CreatePlayerModal
          show={showCreatePlayer}
          handleClose={() => setShowCreatePlayer(false)}
          onCreated={loadPlayers}
        />
        <CreateMasterModal
          show={showCreateMaster}
          handleClose={() => setShowCreateMaster(false)}
          onCreated={loadMasters}
        />      
        <CreateSessionModal
          show={showCreateSession}
          handleClose={() => setShowCreateSession(false)}
          onCreated={loadSessions}
          />  
        <UpdateSessionModal
            show={showUpdateSession}
            handleClose={() => {
            setShowUpdateSession(false);
            setSelectedSession(null);
            }}
            session={selectedSession}
            onUpdated={loadSessions}
        />
        <UpdatePlayerModal
          show={showUpdatePlayer}
          handleClose={() => {
            setShowUpdatePlayer(false);
            setSelectedPlayer(null);
            }}
          player={selectedPlayer}
          onUpdated={loadPlayers}
        />
        <UpdateMasterModal
            show={showUpdateMaster}
            handleClose={() => {
                setShowUpdateMaster(false);
                setSelectedMaster(null);
            }}
            master={selectedMaster}
            onUpdated={loadMasters}
        />
        <MasterDecisionModal
          show={decision.show}
          mode={decision.mode}
          master={decision.master}
          busy={approving !== null && approving === decision.master?.masterId}
          onClose={closeDecision}
          onConfirm={handleDecisionConfirm}
        />
        <ConfirmDeleteModal
          show={deleteModal.show}
          onHide={closeDelete}
          title={deleteTitle}
          body={deleteBody}
          loading={deleting}
          onConfirm={doDelete}
        />
      </Container>
    </>
  );
}
