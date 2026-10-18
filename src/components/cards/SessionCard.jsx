import { Card, Button } from "react-bootstrap";
import { Link } from "react-router-dom";
import { StateBadge, SeatsBadge } from "../common/Badges";
import { normalizeSession, STATE } from "../../utils/homeSessions";

export default function SessionCard({ session }) {
  const { seats, state } = normalizeSession(session);
  const finished = state === STATE.Completed || state === STATE.Cancelled;

  const scheduled = session.scheduledAt
    ? new Date(session.scheduledAt).toLocaleString()
    : "—";

  return (
    <Card className="h-100 shadow-sm border-1">
      {session.coverImgUrl && (
        <Card.Img
          variant="top"
          src={session.coverImgUrl}
          style={{ height: 160, objectFit: "cover" }}
        />
      )}

      <Card.Body className="d-flex flex-column" style={{ color: "var(--muted)" }}>
        <div className="d-flex justify-content-between align-items-start gap-2">
          <Card.Title className="fs-5 mb-1">{session.sessionTitle}</Card.Title>
          <StateBadge state={session.currentState} className="ms-auto flex-shrink-0" />
        </div>

        <Card.Text style={{ color: "var(--muted)" }} className=" small mb-2">
          {scheduled} • durata: {session.duration}
        </Card.Text>

        <Card.Text className="mb-3" style={{ overflow: "hidden" }}>
          {(session.sessionDescription || "").slice(0, 90)}
          {(session.sessionDescription || "").length > 90 ? "…" : ""}
        </Card.Text>

        <div className="mt-auto d-flex justify-content-between align-items-center gap-2">
          <SeatsBadge seats={seats} finished={finished} />

          <Button
            as={Link}
            to={`/sessions/${session.sessionId}`}
            variant="primary"
            size="sm"
          >
            Partecipa
          </Button>
        </div>
      </Card.Body>
    </Card>
  );
}
