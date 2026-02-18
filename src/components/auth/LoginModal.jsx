import { Modal, Button, Form } from "react-bootstrap";
import { useState, useContext } from "react";
import { AuthContext } from "../../context/AuthContext";
import { ToastContext } from "../../context/ToastContext";

export default function LoginModal({ show, handleClose, openRegisterPlayer, openRegisterMaster }) {
  const { login } = useContext(AuthContext);
  const { showToast } = useContext(ToastContext);

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");

  const handleLogin = async (e) => {
    e.preventDefault();
    try {
      await login(email, password);
      showToast("Login effettuato con successo ", "success");
      handleClose();
    } catch {
      showToast("Credenziali non valide", "danger");
    }
  };

  return (
    <Modal 
      show={show}
      onHide={handleClose}
      centered
      animation   
    >
      <Modal.Header closeButton>
        <Modal.Title style={{ color: "var(--muted)" }}>Accedi</Modal.Title>
      </Modal.Header>

      <Modal.Body style={{ color: "var(--muted)" }}>
        <Form onSubmit={handleLogin}>
          <Form.Group className="mb-3">
            <Form.Label>Email</Form.Label>
            <Form.Control type="email" required onChange={(e) => setEmail(e.target.value)} />
          </Form.Group>

          <Form.Group className="mb-3">
            <Form.Label>Password</Form.Label>
            <Form.Control type="password" required onChange={(e) => setPassword(e.target.value)} />
          </Form.Group>

          <Button type="submit" className="w-100 mb-2">
            Login
          </Button>
        </Form>

        <div className="text-center">
  <small>
    Non hai un account?{" "}
    <span className="text-primary" style={{ cursor: "pointer" }} onClick={openRegisterPlayer}>
      Registrati Player
    </span>
    {" • "}
    <span className="text-primary" style={{ cursor: "pointer" }} onClick={openRegisterMaster}>
      Registrati Master
    </span>
  </small>
</div>
      </Modal.Body>
    </Modal>
  );
}
