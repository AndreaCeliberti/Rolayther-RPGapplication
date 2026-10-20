
import { BrowserRouter, Routes, Route } from "react-router-dom";
import { useState } from "react";

import "./api/axios"

import { AuthProvider } from "./context/AuthContext";
import { ToastProvider } from "./context/ToastContext";

import AppNavbar from "./components/AppNavbar";

import LoginModal from "./components/auth/LoginModal";
import RegisterPlayerModal from "./components/auth/RegisterPlayerModal";
import RegisterMasterModal from "./components/auth/RegisterMasterModal";
import CheckEmailModal from "./components/auth/CheckEmailModal";
import EmailConfirmBanner from "./components/auth/EmailConfirmBanner";
import LoginLinkHandler from "./components/auth/LoginLinkHandler";

import Home from "./pages/Home";
import SessionDetails from "./pages/SessionDetails";
import Sessions from "./pages/Sessions";
import Masters from "./pages/Masters";
import Games from "./pages/Games";
import Genres from "./pages/Genres";
import Platforms from "./pages/Platforms";
import PlayerProfile from "./pages/PlayerProfile";
import MasterProfile from "./pages/MasterProfile";
import AdminDashboard from "./pages/AdminDashboard";
import CreateSessionModal from "./components/admin/CreateSessionModal";
import AdminRoute from "./components/admin/AdminRoute";
import MasterDetails from "./pages/MasterDetails";
import ConfirmEmailPage from "./pages/ConfirmEmailPage";
import PrivacyPolicy from "./pages/PrivacyPolicy";
import AppFooter from "./components/AppFooter";

export default function App() {
  const [showLogin, setShowLogin] = useState(false);
  const [showRegPlayer, setShowRegPlayer] = useState(false);
  const [showRegMaster, setShowRegMaster] = useState(false);

  const openLogin = () => setShowLogin(true);
  const closeLogin = () => setShowLogin(false);

  const openRegPlayer = () => setShowRegPlayer(true);
  const closeRegPlayer = () => setShowRegPlayer(false);

  const openRegMaster = () => setShowRegMaster(true);
  const closeRegMaster = () => setShowRegMaster(false);
  const [showCreateSession, setShowCreateSession] = useState(false);

  // Dopo la registrazione: finestra "Controlla la tua email" (a chi è stata scritta, se l'email è partita, se è una richiesta Master)
  const [checkEmail, setCheckEmail] = useState({ show: false, email: "", emailSent: true, isMaster: false });
  const openCheckEmail = (info) => setCheckEmail({ show: true, ...info });
  const closeCheckEmail = () => setCheckEmail((c) => ({ ...c, show: false }));

  return (
    <AuthProvider>
      <ToastProvider>
        <BrowserRouter>
          <div className="app-shell theme-dark">
    <AppNavbar
      openLogin={() => setShowLogin(true)}
      openRegisterPlayer={() => setShowRegPlayer(true)}
      openRegisterMaster={() => setShowRegMaster(true)}
      openCreateSession={() => setShowCreateSession(true)}
    />

    {/* Avviso per chi non ha ancora confermato l'email (con "Reinvia email") */}
    <EmailConfirmBanner />

    {/* I pulsanti "Accedi" delle email puntano a /?login=1 */}
    <LoginLinkHandler openLogin={openLogin} />

    {/* MODALI GLOBALI */}
    <LoginModal
      show={showLogin}
      handleClose={closeLogin}
      openRegisterPlayer={() => {
        closeLogin();
        openRegPlayer();
      }}
      openRegisterMaster={() => {
        closeLogin();
        openRegMaster();
      }}
    />

    <RegisterPlayerModal
      show={showRegPlayer}
      handleClose={closeRegPlayer}
      openLogin={() => {
        closeRegPlayer();
        openLogin();
      }}
      onRegistered={openCheckEmail}
    />

    <RegisterMasterModal
      show={showRegMaster}
      handleClose={closeRegMaster}
      openLogin={() => {
        closeRegMaster();
        openLogin();
      }}
      onRegistered={openCheckEmail}
    />

    <CheckEmailModal
      show={checkEmail.show}
      handleClose={closeCheckEmail}
      email={checkEmail.email}
      emailSent={checkEmail.emailSent}
      isMaster={checkEmail.isMaster}
      openLogin={openLogin}
    />

    <CreateSessionModal
      show={showCreateSession}
      handleClose={() => setShowCreateSession(false)}
    />

    {/* CONTENUTO PAGINE */}
    <main className="app-main">
      <Routes>
        <Route
          path="/"
          element={
            <Home
              openLogin={openLogin}
              openRegisterPlayer={openRegPlayer}
              openRegisterMaster={openRegMaster}
              openCreateSession={() => setShowCreateSession(true)}
            />
          }
        />
        <Route path="/sessions/:id" element={<SessionDetails />} />
        <Route path="/sessions" element={<Sessions />} />
        <Route path="/masters" element={<Masters />} />
        <Route path="/games" element={<Games />} />
        <Route path="/genres" element={<Genres />} />
        <Route path="/platforms" element={<Platforms />} />
        <Route path="/playerProfile" element={<PlayerProfile />} />
        <Route path="/masterProfile" element={<MasterProfile />} />
        <Route path="/masters/:id" element={<MasterDetails />} />
        <Route path="/confirm-email" element={<ConfirmEmailPage openLogin={openLogin} />} />
        <Route path="/privacy" element={<PrivacyPolicy />} />
        <Route
          path="/admin"
          element={
            <AdminRoute>
              <AdminDashboard />
            </AdminRoute>
          }
        />
      </Routes>
    </main>

    <AppFooter />
          </div>
        </BrowserRouter>

      </ToastProvider>
    </AuthProvider>
  );
}
