
import axios from "axios";

// Evento lanciato quando il server risponde "devi prima confermare la tua email" (403, code EmailNotConfirmed).
// AuthContext lo ascolta e mostra subito l'avviso con il pulsante "Reinvia email".
export const EMAIL_UNCONFIRMED_EVENT = "rolayther:email-unconfirmed";

const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL,
  withCredentials: true,
});

// ===== REQUEST INTERCEPTOR =====
api.interceptors.request.use((config) => {
  const token = localStorage.getItem("accessToken");
  if (token) {
    config.headers = config.headers ?? {};
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// ===== RESPONSE INTERCEPTOR =====
api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    // Iscrizione a una sessione / creazione sessione senza email confermata
    if (error.response?.status === 403 && error.response.data?.code === "EmailNotConfirmed") {
      window.dispatchEvent(new Event(EMAIL_UNCONFIRMED_EVENT));
    }

    if (error.response?.status === 401 && !originalRequest?._retry) {
      originalRequest._retry = true;

      try {
        const refreshToken = localStorage.getItem("refreshToken");
        const res = await api.post("/Auth/refresh", { refreshToken });

        localStorage.setItem("accessToken", res.data.accessToken);
        if (typeof res.data.emailConfirmed === "boolean") {
          localStorage.setItem("emailConfirmed", String(res.data.emailConfirmed));
        }

        originalRequest.headers = originalRequest.headers ?? {};
        originalRequest.headers.Authorization = `Bearer ${res.data.accessToken}`;

        return api(originalRequest);
      } catch {
        localStorage.clear();
        return Promise.reject(error);
      }
    }

    return Promise.reject(error);
  }
);

export default api;
