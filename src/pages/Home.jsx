import { useContext, useEffect, useMemo, useState } from "react";
import { Container, Row, Col, Button, Spinner } from "react-bootstrap";
import { SessionsApi } from "../api/sessions.api";
import SessionCard from "../components/cards/SessionCard";
import { AuthContext } from "../context/AuthContext";
import { ToastContext } from "../context/ToastContext";
import CreateSessionModal from "../components/admin/CreateSessionModal";
import { apiErrorMessage } from "../utils/apiError";

export default function Home() {
  const { user } = useContext(AuthContext);
  const { showToast } = useContext(ToastContext);

  const [sessions, setSessions] = useState([]);
  const [loading, setLoading] = useState(true);
  const [showCreate, setShowCreate] = useState(false);

  const canCreate = useMemo(
    () => user && (user.role === "Admin" || user.role === "Master"),
    [user]
  );

  const loadSessions = async () => {
    setLoading(true);
    try {
      const res = await SessionsApi.getAll();
      setSessions(res.data || []);
    } catch {
      showToast("Errore nel caricamento sessioni", "danger");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadSessions();
  }, []);

  const handleJoin = async (sessionId) => {
    if (!user) return showToast("Devi fare login per partecipare", "warning");
    if (!user.playerId) return showToast("Serve un profilo Player per fare Join", "warning");

    try {
      await SessionsApi.join(sessionId, user.playerId);
      showToast("Sei entrato nella sessione 🎉", "success");
      await loadSessions();
    } catch (err) {
      // il server spiega il motivo (es. "devi prima confermare la tua email")
      showToast(apiErrorMessage(err, "Impossibile entrare nella sessione"), "danger", 6000);
    }
  };

  const handleLeave = async (sessionId) => {
    if (!user?.playerId) return;

    try {
      await SessionsApi.leave(sessionId, user.playerId);
      showToast("Hai lasciato la sessione", "success");
      await loadSessions();
    } catch {
      showToast("Impossibile lasciare la sessione", "danger");
    }
  };

  return (
    <Container fluid="md" className="mt-4 px-3 px-md-0">
      <div className="d-flex flex-column flex-md-row justify-content-between align-items-md-center gap-2">
        <div>
          <h1 className="mb-1 text-center text-md-start">Benvenuto!</h1>
          <p style={{ color: "var(--muted)" }} className=" text-center text-md-start mb-0">
            Unisciti ai <strong>Rolayther</strong> e vivi un avventura!
          </p>
        </div>

        
      </div>

      {loading ? (
        <div className="text-center mt-5">
          <Spinner animation="border" />
        </div>
      ) : (
        <Row className="g-4 mt-3">
          {sessions.map((s) => {
            const players = s.players || [];
            const playersCount = players.length;
            const maxPlayers = s.numbOfPlayer ?? 0;
            const isFull = maxPlayers > 0 && playersCount >= maxPlayers;

            const isInSession =
              user?.playerId &&
              players.some((p) => p.playerId === user.playerId);

            const canJoin = !!user?.playerId && !isInSession && !isFull;
            const canLeave = !!user?.playerId && isInSession;

            return (
              <Col key={s.sessionId} xs={12} sm={6} lg={4} xl={3}>
                <SessionCard
                  session={s}
                  isFull={isFull}
                  canJoin={canJoin}
                  canLeave={canLeave}
                  onJoin={handleJoin}
                  onLeave={handleLeave}
                />
              </Col>
            );
          })}
        </Row>
      )}

      <CreateSessionModal
        show={showCreate}
        onHide={() => setShowCreate(false)}
        onCreated={loadSessions}
      />
    </Container>
  );
}
