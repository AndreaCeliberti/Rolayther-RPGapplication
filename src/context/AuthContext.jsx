import { createContext, useCallback, useEffect, useState } from "react";
import { jwtDecode } from "jwt-decode";
import api, { EMAIL_UNCONFIRMED_EVENT } from "../api/axios";
import { PlayersApi } from "../api/players.api";
import { AuthApi } from "../api/auth.api";

export const AuthContext = createContext();

// Utente salvato nel browser: ripristino al refresh pagina (subito, senza un "primo render" da sloggato)
function readStoredUser() {
  const token = localStorage.getItem("accessToken");
  const role = localStorage.getItem("role");
  const playerId = localStorage.getItem("playerId");

  if (!token || !role) return null;

  try {
    const decoded = jwtDecode(token);
    return {
      role,
      email: decoded.email,
      playerId: playerId || null,
      // Mai salvato (sessione aperta prima di questa funzione): si assume "confermata" finché il server non dice altro
      emailConfirmed: localStorage.getItem("emailConfirmed") !== "false",
    };
  } catch {
    localStorage.clear();
    return null;
  }
}

// true/false = com'è l'email dell'utente collegato secondo il server (lancia se il server non risponde)
async function fetchEmailConfirmed() {
  const res = await AuthApi.emailStatus();
  return !!res.data?.emailConfirmed;
}

export const AuthProvider = ({ children }) => {
  const [user, setUser] = useState(readStoredUser);

  const saveEmailConfirmed = useCallback((confirmed) => {
    localStorage.setItem("emailConfirmed", String(confirmed));
    setUser((u) => (u && u.emailConfirmed !== confirmed ? { ...u, emailConfirmed: confirmed } : u));
  }, []);

  // Chiede al server se l'email dell'utente collegato è confermata. Restituisce true/false,
  // oppure null se non si può sapere (nessun utente collegato, server non raggiungibile).
  const refreshEmailStatus = useCallback(async () => {
    if (!localStorage.getItem("accessToken")) return null;

    try {
      const confirmed = await fetchEmailConfirmed();
      saveEmailConfirmed(confirmed);
      return confirmed;
    } catch {
      return null;
    }
  }, [saveEmailConfirmed]);

  // Dopo un ripristino/accesso si controlla lo stato vero (può essere cambiato da un altro dispositivo)
  const statusCheckFor = user && user.role !== "Admin" ? user.email : null;
  useEffect(() => {
    if (!statusCheckFor) return undefined;

    let cancelled = false;
    fetchEmailConfirmed()
      .then((confirmed) => {
        if (!cancelled) saveEmailConfirmed(confirmed);
      })
      .catch(() => {
        // server non raggiungibile: resta lo stato che si conosceva
      });

    return () => {
      cancelled = true;
    };
  }, [statusCheckFor, saveEmailConfirmed]);

  // Il server ha risposto "prima conferma la tua email": l'avviso con "Reinvia email" compare subito
  useEffect(() => {
    const onUnconfirmed = () => saveEmailConfirmed(false);
    window.addEventListener(EMAIL_UNCONFIRMED_EVENT, onUnconfirmed);
    return () => window.removeEventListener(EMAIL_UNCONFIRMED_EVENT, onUnconfirmed);
  }, [saveEmailConfirmed]);

  const login = async (email, password) => {
    const res = await api.post("/Auth/login", { email, password });
    const data = res.data;

    // Risposta senza il campo (server più vecchio): si assume "confermata", non si blocca nessuno per errore
    const emailConfirmed = data.emailConfirmed !== false;

    localStorage.setItem("accessToken", data.accessToken);
    localStorage.setItem("refreshToken", data.refreshToken);
    localStorage.setItem("role", data.role);
    localStorage.setItem("emailConfirmed", String(emailConfirmed));

    const decoded = jwtDecode(data.accessToken);
    //console.log("DECODED TOKEN:", decoded);

    // se è Player recupera playerId via /Player/Me
    let playerId = null;
    if (data.role === "Player") {
      const me = await PlayersApi.getMe();
      playerId = me.data.playerId;
      localStorage.setItem("playerId", playerId);
      //console.log("ME DATA:", me.data);
    } else {
      localStorage.removeItem("playerId");
    }

    setUser({ role: data.role, email: decoded.email, playerId, emailConfirmed });
  };

  const logout = async () => {
    const refreshToken = localStorage.getItem("refreshToken");
    try {
      await api.post("/Auth/logout", { refreshToken });
    } catch {
      // anche se il backend risponde 401 o errore, puliamo lato client comunque
    }
    localStorage.clear();
    setUser(null);
  };

  // L'Admin non ha bisogno di conferma: l'avviso riguarda solo Player, Master e Master in attesa
  const needsEmailConfirmation = !!user && user.role !== "Admin" && user.emailConfirmed === false;

  return (
    <AuthContext.Provider value={{ user, login, logout, needsEmailConfirmation, refreshEmailStatus }}>
      {children}
    </AuthContext.Provider>
  );
};
