import { Modal, Button, Spinner } from "react-bootstrap";

export default function ConfirmDeleteModal({
  show,
  onHide,
  title = "Conferma eliminazione",
  body,
  confirmText = "Elimina",
  cancelText = "Annulla",
  loading = false,
  danger = true,
  onConfirm,
}) {
  return (
    <Modal
      show={show}
      onHide={loading ? undefined : onHide}
      centered
      animation
      backdrop="static"
      contentClassName="border-1 shadow"
    >
      <Modal.Header closeButton={!loading} className="border-1 pb-0">
        <Modal.Title className="fw-semibold text-white">{title}</Modal.Title>
      </Modal.Header>

      <Modal.Body className="pt-2">
        {body ? (
          <div style={{ color: "var(--muted)" }}>{body}</div>
        ) : (
          <div style={{ color: "var(--muted)" }}>
            Sei sicuro? Questa operazione non è reversibile.
          </div>
        )}
      </Modal.Body>

      <Modal.Footer className="border-0">
        <Button variant="outline-secondary" onClick={onHide} disabled={loading}>
          {cancelText}
        </Button>

        <Button
          variant={danger ? "danger" : "primary"}
          onClick={onConfirm}
          disabled={loading}
        >
          {loading ? (
            <>
              <Spinner size="sm" className="me-2" />
              Eliminazione…
            </>
          ) : (
            confirmText
          )}
        </Button>
      </Modal.Footer>
    </Modal>
  );
}
