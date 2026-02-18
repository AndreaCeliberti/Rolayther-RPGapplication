import { useContext, useEffect, useMemo, useState, useRef } from "react";
import { useParams, Link } from "react-router-dom";
import {
  Container,
  Row,
  Col,
  Card,
  Button,
  Badge,
  Spinner,
  ListGroup,
} from "react-bootstrap";

import PageHeader from "../components/common/PageHeader";
import EmptyState from "../components/common/EmptyState";
import { ToastContext } from "../context/ToastContext";
import { AuthContext } from "../context/AuthContext";
import { stateBadgeVariant, stateLabel } from "../utils/sessionState";
import { SessionsApi } from "../api/sessions.api";
import { PlayersApi } from "../api/players.api";

export default function SessionDetails() {
  const { id } = useParams(); // sessionId (Guid)
  const { showToast } = useContext(ToastContext);
  const { user } = useContext(AuthContext);

  const [loading, setLoading] = useState(true);
  const [session, setSession] = useState(null);

  const [meLoading, setMeLoading] = useState(false);
  const [mePlayerId, setMePlayerId] = useState(null);

  const [joining, setJoining] = useState(false);
  const [leaving, setLeaving] = useState(false);

  const isPlayer = user?.role === "Player";

  const loadSession = async () => {
    setLoading(true);
    try {
      
      const res = await SessionsApi.getById(id);
      
      setSession(res.data || null);
    } catch (err) {
      const msg =
        err?.response?.data?.message ||
        err?.response?.data?.Message ||
        "Errore nel caricamento della sessione";
      showToast(msg, "danger");
      setSession(null);
    } finally {
      setLoading(false);
    }
  };

  // carica sessione
  useEffect(() => {
    loadSession();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id]);

  // se sei Player loggato -> recupera PlayerId dal backend
  useEffect(() => {
  if (!isPlayer) {
    setMePlayerId(null);
    return;
  }

  setMePlayerId(user?.playerId || user?.PlayerId || null);
}, [isPlayer, user]);

  const players = session?.players || session?.Players || [];
  const maxPlayers = session?.numbOfPlayer ?? session?.NumbOfPlayer ?? 0;
  const currentState = session?.currentState ?? session?.CurrentState; // numero enum
  const isPublished = currentState === 1; // SessionState.Published = 1


  const joined = useMemo(() => {
    if (!mePlayerId) return false;
    return players.some((p) => (p.playerId || p.PlayerId) === mePlayerId);
  }, [players, mePlayerId]);

  const seatsTaken = players.length;
  const seatsAvailable = Math.max(0, (maxPlayers || 0) - seatsTaken);
  const isFull = maxPlayers > 0 && seatsTaken >= maxPlayers;

  const sessionLink = session?.sessionLink || session?.SessionLink;  

  const canJoin = isPlayer && !!mePlayerId && isPublished && !joined && !isFull;
  const canLeave = isPlayer && !!mePlayerId && joined;

const joinLock = useRef(false);

const handleJoin = async () => {
  if (!canJoin) return;
  if (joinLock.current) return;     // blocco duro
  joinLock.current = true;

  setJoining(true);
  try {
    await SessionsApi.join(id, mePlayerId);
    showToast("Sei entrato nella sessione", "success");
    await loadSession();
  } catch (err) {
    const msg =
      err?.response?.data?.message ||
      err?.response?.data?.Message ||
      "Impossibile entrare nella sessione";

    showToast(msg, "danger");
    await loadSession();
  } finally {
    setJoining(false);
    joinLock.current = false;       //  sblocco
  }
};

  const handleLeave = async () => {
    if (!canLeave) return;
    setLeaving(true);
    try {
      await SessionsApi.leave(id, mePlayerId);
      showToast("Hai lasciato la sessione", "success");
      await loadSession();
    } catch (err) {
      const msg =
        err?.response?.data?.message ||
        err?.response?.data?.Message ||
        "Impossibile lasciare la sessione";
      showToast(msg, "danger");
    } finally {
      setLeaving(false);
    }
  };

  return (
    <>
      <PageHeader
        title="Dettagli Sessione"
        subtitle="Informazioni complete e partecipazione."
        actions={
          <Button as={Link} to="/sessions" variant="outline-secondary">
             Torna alle sessioni
          </Button>
        }
      />

      <Container fluid="md" className="pb-4 px-3 px-md-0">
        {loading ? (
          <div className="text-center mt-5">
            <Spinner animation="border" />
          </div>
        ) : !session ? (
          <EmptyState
            icon="🕵️‍♂️"
            title="Sessione non trovata"
            hint="Potrebbe essere stata rimossa o l’ID non è corretto."
            action={
              <Button as={Link} to="/sessions">
                Vai alla lista sessioni
              </Button>
            }
          />
        ) : (
          <Row className="g-4">
            {/* COL: cover + info */}
            <Col xs={12} lg={8}>
              <Card className="border-0 shadow-sm overflow-hidden">
                {session.coverImgUrl && (
                  <div style={{ maxHeight: 260, overflow: "hidden" }}>
                    <img
                      src={session.coverImgUrl}
                      alt="cover"
                      style={{ width: "100%", height: "100%", objectFit: "cover" }}
                    />
                  </div>
                )}

                <Card.Body>
                  <div className="d-flex flex-column flex-sm-row justify-content-between gap-2">
                    <div>
                      <h4 className="mb-1">{session.sessionTitle}</h4>
                      <div className="text-muted small">
                        {session.scheduledAt
                          ? new Date(session.scheduledAt).toLocaleString()
                          : "Data non disponibile"}
                        {" • "}
                        {session.duration}
                      </div>

                      <div className="mt-2 d-flex gap-2 flex-wrap">
                        {session.game?.title && (
                          <Badge bg="secondary"> {session.game.title}</Badge>
                        )}
                        {session.genre?.name && (
                          <Badge bg="secondary"> {session.genre.name}</Badge>
                        )}
                        
                        {session.master?.nickName && (
                          <Badge bg="secondary">Mastered by {session.master.nickName}</Badge>
                        )}
                      <div className=" d-flex align-items-center gap-2">
                        <Badge bg={stateBadgeVariant(session.currentState)}>
                          {stateLabel(session.currentState)}
                        </Badge>
                      </div>
                      </div>
                    </div>
                    
                    <div className="text-sm-end">
                      
                      <Badge bg={isFull ? "danger" : "success"} className="fs-6">
                        👥 {seatsTaken}/{maxPlayers}
                      </Badge>
                      <div className="text-muted small mt-1">
                        {isFull ? "Posti esauriti" : `${seatsAvailable} posti disponibili`}
                      </div>
                    </div>
                  </div>

                  <hr className="my-3" />

                  <div className="text-muted" style={{ whiteSpace: "pre-wrap" }}>
                    {session.sessionDescription || "Nessuna descrizione disponibile."}
                  </div>

                  <hr className="my-3" />

                  {/* Join/Leave area */}
                  {!user ? (
                    <div className="text-muted small">
                      Effettua il login come Player per partecipare.
                    </div>
                  ) : !isPlayer ? (
                    <div className="text-muted small">
                      Sei loggato come <b>{user.role}</b>. Solo i Player possono fare Join/Leave.
                    </div>
                  ) : meLoading ? (
                    <div className="text-muted small">Caricamento profilo…</div>
                  ) : (
                    <div className="d-flex flex-column flex-sm-row gap-2">
                     
                      <Button
                        variant="success"
                        onClick={handleJoin}
                        disabled={!canJoin || joining}
                      >
                        {joining ? (
                          <>
                            <Spinner size="sm" className="me-2" />
                            Join…
                          </>
                        ) : (
                          " Join sessione"
                        )}
                      </Button>

                      <Button
                        variant="outline-danger"
                        onClick={handleLeave}
                        disabled={!canLeave || leaving}
                      >
                        {leaving ? (
                          <>
                            <Spinner size="sm" className="me-2" />
                            Leave…
                          </>
                        ) : (
                          "Lascia sessione"
                        )}
                      </Button>

                      {joined && (
                        <div className="text-muted small align-self-center">
                          Sei già iscritto a questa sessione.
                        </div>
                      )}
                      
                    </div>
                    
                  )}
                  {joined && sessionLink && (
                      <div className="mt-3 d-flex flex-wrap gap-2">
                        <Button
                          as="a"
                          href={sessionLink}
                          target="_blank"
                          rel="noreferrer"
                          variant="outline-primary"
                        >
                           Partecipa alla Sessione
                        </Button>
                      </div>
                    )}

                </Card.Body>
              </Card>
            </Col>

            {/* COL: lista player */}
            <Col xs={12} lg={4}>
              <Card className="border-0 shadow-sm">
                <Card.Body>
                  <h5 className="mb-2">Partecipanti</h5>
                  <div className="text-muted small mb-3">
                    {seatsTaken} iscritti • {seatsAvailable} posti disponibili
                  </div>

                  {players.length === 0 ? (
                    <div className="text-muted small">
                      Nessun player iscritto per ora.
                    </div>
                  ) : (
                    <ListGroup variant="flush" className="rol-players bg-dark">
                      {players.map((p) => {
                        const pid = p.playerId || p.PlayerId;

                        const label =
                          p.nickName ||
                          p.NickName ||
                          `${p.name ?? ""} ${p.surname ?? ""}`.trim() ||
                          "Player";

                        const mine = mePlayerId && pid === mePlayerId;

                        return (
                          <ListGroup.Item
                            key={pid}
                            className={`rol-item px-0 py-2 d-flex align-items-center ${mine ? "is-mine" : ""}`}
                          >
                            <div className="d-flex justify-content-between align-items-center w-100 rol-item">
                              <div className="d-flex align-items-center gap-2 text-truncate" style={{ maxWidth: 240 }}>
                                {mine && <span className="rol-star" aria-hidden="true">★</span>}
                                <span className="rol-name">{label}</span>
                              </div>

                              {mine && (
                                <Badge pill bg="primary" className="rol-badge">
                                  Tu
                                </Badge>
                              )}
                            </div>
                          </ListGroup.Item>
                        );
                      })}
                    </ListGroup>

                  )}
                </Card.Body>
              </Card>
            </Col>
          </Row>
        )}
      </Container>
    </>
  );
}
