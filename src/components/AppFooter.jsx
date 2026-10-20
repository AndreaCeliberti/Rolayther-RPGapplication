import { Container } from "react-bootstrap";
import { Link } from "react-router-dom";
import {
  FaInstagram,
  FaGithub,
  FaLinkedin,
  FaFacebook,
  FaYoutube,
} from "react-icons/fa";

export default function AppFooter() {
  const year = new Date().getFullYear();

  return (
    <footer
      className="mt-auto"
      style={{
        borderTop: "1px solid rgba(255,255,255,.06)",
        background: "rgba(10,7,18,.55)",
        backdropFilter: "blur(10px)",
      }}
    >
      <Container className="py-4">
        <div className="d-flex flex-column flex-md-row align-items-center justify-content-between gap-3">
          {/* Brand + copyright */}
          <div className="text-center text-md-start">
            <div className="fw-semibold" style={{ color: "var(--text)" }}>
              Rolayther
            </div>
            <div className="small" style={{ color: "var(--muted)" }}>
              © {year} • Tutti i diritti riservati
            </div>
          </div>

          {/* Links interni */}
          <div className="d-flex flex-wrap justify-content-center gap-3 small">
            <FooterLink to="/sessions">Sessioni</FooterLink>
            <FooterLink to="/masters">Masters</FooterLink>
            <FooterLink to="/games">Games</FooterLink>
            <FooterLink to="/genres">Genres</FooterLink>
            <FooterLink to="/platforms">Platforms</FooterLink>
            <FooterLink to="/privacy">Privacy</FooterLink>
          </div>

          {/* Social */}
          <div className="d-flex align-items-center justify-content-center gap-2">
            <SocialIcon href="https://instagram.com" label="Instagram">
              <FaInstagram />
            </SocialIcon>
            <SocialIcon href="https://github.com" label="GitHub">
              <FaGithub />
            </SocialIcon>
            <SocialIcon href="https://linkedin.com" label="LinkedIn">
              <FaLinkedin />
            </SocialIcon>
            <SocialIcon href="https://facebook.com" label="Facebook">
              <FaFacebook />
            </SocialIcon>
            <SocialIcon href="https://youtube.com" label="YouTube">
              <FaYoutube />
            </SocialIcon>
          </div>
        </div>
      </Container>
    </footer>
  );
}

function FooterLink({ to, children }) {
  return (
    <Link
      to={to}
      className="text-decoration-none"
      style={{ color: "var(--muted)" }}
      onMouseEnter={(e) => (e.currentTarget.style.color = "var(--text)")}
      onMouseLeave={(e) => (e.currentTarget.style.color = "var(--muted)")}
    >
      {children}
    </Link>
  );
}

function SocialIcon({ href, label, children }) {
  return (
    <a
      href={href}
      target="_blank"
      rel="noreferrer"
      aria-label={label}
      title={label}
      className="d-inline-flex align-items-center justify-content-center"
      style={{
        width: 40,
        height: 40,
        borderRadius: "999px",
        border: "1px solid rgba(167,120,255,.18)",
        background: "rgba(139,92,246,.08)",
        color: "var(--text)",
        textDecoration: "none",
        transition: "transform .16s ease, box-shadow .16s ease, border-color .16s ease",
        boxShadow: "0 10px 24px rgba(0,0,0,.25)",
      }}
      onMouseEnter={(e) => {
        e.currentTarget.style.transform = "translateY(-1px)";
        e.currentTarget.style.boxShadow =
          "0 12px 30px rgba(0,0,0,.40), 0 0 26px rgba(139,92,246,.22)";
        e.currentTarget.style.borderColor = "rgba(167,120,255,.35)";
      }}
      onMouseLeave={(e) => {
        e.currentTarget.style.transform = "translateY(0)";
        e.currentTarget.style.boxShadow = "0 10px 24px rgba(0,0,0,.25)";
        e.currentTarget.style.borderColor = "rgba(167,120,255,.18)";
      }}
    >
      <span style={{ fontSize: 18, lineHeight: 0 }}>{children}</span>
    </a>
  );
}
