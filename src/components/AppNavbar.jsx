import { useContext } from "react";
import {
  Navbar,
  Nav,
  Container,
  NavDropdown,
  Button,
  Badge,
} from "react-bootstrap";
import { NavLink, useNavigate } from "react-router-dom";
import { AuthContext } from "../context/AuthContext";
import logo from "../assets/InsegnaRolayther.png";

function roleBadgeVariant(role) {
  if (role === "Admin") return "warning";
  if (role === "Master") return "info";
  return "success"; // Player
}

export default function AppNavbar({
  openLogin,
  openRegisterPlayer,
  openRegisterMaster,
  openCreateSession, 
}) {
  const { user, logout } = useContext(AuthContext);
  const navigate = useNavigate();

  const handleLogout = async () => {
    try {
      await logout();
    } finally {
      navigate("/");
    }
  };

  const canCreateSession = user?.role === "Admin" || user?.role === "Master";

  const profilePath =
    user?.role === "Player"
      ? "/playerProfile"
      : user?.role === "Master"
      ? "/masterProfile"
      : "/admin";

  return (
    <Navbar bg="dark" variant="dark" expand="lg" sticky="top" className="shadow-sm">
      <Container>
        <Navbar.Brand
          as={NavLink}
          to="/"
          className="d-flex align-items-center"
        >
          <img
            src={logo}
            alt="Rolayther Logo"
            className="img-fluid"
            style={{ maxHeight: "50px" }}
          />
        </Navbar.Brand>

        <Navbar.Toggle aria-controls="main-navbar" />
        <Navbar.Collapse id="main-navbar">
          {/* LEFT */}
          <Nav className="me-auto nav-folders">
            <Nav.Link as={NavLink} to="/sessions" end>
              Sessioni
            </Nav.Link>
            <Nav.Link as={NavLink} to="/masters" end>
              Master
            </Nav.Link>
            <Nav.Link as={NavLink} to="/games" end>
              Giochi
            </Nav.Link>
            <Nav.Link as={NavLink} to="/genres" end>
              Generi
            </Nav.Link>
            <Nav.Link as={NavLink} to="/platforms" end>
              Piattaforme
            </Nav.Link>

            {user?.role === "Admin" && (
              <Nav.Link as={NavLink} to="/admin" end className="fw-semibold">
                Admin
              </Nav.Link>
            )}
          </Nav>

          {/* RIGHT */}
          <Nav className="ms-auto align-items-lg-center gap-2">
            {/*  Button Create Session only Admin/Master */}
            {canCreateSession && (
              <Button
                size="sm"
                variant="outline-light"
                className="me-lg-2"
                onClick={openCreateSession}
              >
                 Crea Sessione
              </Button>
            )}

            {!user ? (
              <>
                <Button variant="outline-light" size="sm" onClick={openLogin}>
                  Login
                </Button>

                <NavDropdown title="Registrati" align="end" menuVariant="dark">
                  <NavDropdown.Item onClick={openRegisterPlayer}>
                     Become a Player
                  </NavDropdown.Item>
                  <NavDropdown.Item onClick={openRegisterMaster}>
                     Be a Master
                  </NavDropdown.Item>
                </NavDropdown>
              </>
            ) : (
              <NavDropdown
                align="end"
                menuVariant="dark"
                title={
                  <span className="d-inline-flex align-items-center gap-2">
                    <span className="text-truncate" style={{ maxWidth: 150 }}>
                      {user.email || "Account"}
                    </span>
                    <Badge bg={roleBadgeVariant(user.role)} text="dark">
                      {user.role}
                    </Badge>
                  </span>
                }
              >
                <NavDropdown.Item as={NavLink} to={profilePath}>
                  {user.role === "Player" && " Profilo "}
                  {user.role === "Master" && " Profilo "}
                  {user.role === "Admin" && " Dashboard Admin"}
                </NavDropdown.Item>

                

                <NavDropdown.Divider />

                <NavDropdown.Item onClick={handleLogout}>
                   Logout
                </NavDropdown.Item>
              </NavDropdown>
            )}
          </Nav>
        </Navbar.Collapse>
      </Container>
    </Navbar>
  );
}
