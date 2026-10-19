import { useEffect, useState, useContext } from "react";
import { useParams } from "react-router-dom";
import { Container, Row, Col, Card, Spinner } from "react-bootstrap";

import { MastersApi } from "../api/masters.api";
import { ToastContext } from "../context/ToastContext";

import PageHeader from "../components/common/PageHeader";
import EmptyState from "../components/common/EmptyState";
import { Chip } from "../components/common/Badges";

export default function MasterDetails() {
  const { id } = useParams();
  const { showToast } = useContext(ToastContext);

  const [master, setMaster] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!id) return;

    const loadMaster = async () => {
      try {
        setLoading(true);
        const res = await MastersApi.getById(id);
        setMaster(res.data);
      } catch (err) {
        showToast(
          err?.response?.data?.message ||
            err?.response?.data?.Message ||
            "Errore caricamento master",
          "danger"
        );
        setMaster(null);
      } finally {
        setLoading(false);
      }
    };

    loadMaster();
  }, [id]);

  if (loading) {
    return (
      <Container className="py-5 text-center">
        <Spinner animation="border" />
      </Container>
    );
  }

  if (!master) {
    return (
      <Container className="py-5">
        <EmptyState
          icon="🧙"
          title="Master non trovato"
          hint="Il profilo richiesto non esiste."
        />
      </Container>
    );
  }

  return (
    <>
      <PageHeader
        title={master.nickName || "Master"}
        subtitle="Profilo Master"
      />

      <Container className="pb-5 px-3 px-md-0">
        <Row className="g-4">
          {/* COLONNA PROFILO */}
          <Col xs={12} md={4}>
            <Card className="border-0 shadow-sm text-center p-4">
              <div
                className="rounded-circle bg-secondary-subtle mx-auto mb-3 d-flex align-items-center justify-content-center"
                style={{ width: 110, height: 110, overflow: "hidden" }}
              >
                {master.avatarImgUrl ? (
                  <img
                    src={master.avatarImgUrl}
                    alt="avatar"
                    style={{
                      width: "100%",
                      height: "100%",
                      objectFit: "cover",
                    }}
                  />
                ) : (
                  <span style={{ fontSize: 40 }}>🧙</span>
                )}
              </div>

              <h5 className="fw-semibold mb-1">
                {master.nickName || "Master"}
              </h5>

              <div className="text-muted small mb-3">
                {master.email}
              </div>

              <Chip>Sessioni: {master.sessions?.length ?? 0}</Chip>
            </Card>
          </Col>

          {/* COLONNA BIO + CONTENUTI */}
          <Col xs={12} md={8}>
            <Card className="border-0 shadow-sm p-4">
              <h6 className="fw-semibold mb-3">Biografia</h6>

              {master.bioMaster ? (
                <p className="text-muted mb-0">
                  {master.bioMaster}
                </p>
              ) : (
                <div className="text-muted small">
                  Nessuna biografia disponibile.
                </div>
              )}
            </Card>

            {/* SESSIONI */}
            <Card className="border-0 shadow-sm p-4 mt-4">
              <h6 className="fw-semibold mb-3">Sessioni create</h6>

              {master.sessions?.length > 0 ? (
                <Row className="g-3">
                  {master.sessions.map((s) => (
                    <Col xs={12} key={s.sessionId}>
                      <Card className="border-0 bg-light">
                        <Card.Body>
                          <div className="fw-semibold">
                            {s.sessionTitle}
                          </div>
                          <div className="small text-muted">
                            {new Date(s.scheduledAt).toLocaleString()}
                          </div>
                        </Card.Body>
                      </Card>
                    </Col>
                  ))}
                </Row>
              ) : (
                <div className="text-muted small">
                  Nessuna sessione creata.
                </div>
              )}
            </Card>
          </Col>
        </Row>
      </Container>
    </>
  );
}
