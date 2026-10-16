import api from "./axios";
import { postFile } from "./upload";

// Route backend: /api/Session/...
export const SessionsApi = {
  getAll: () => api.get("/Session/GetAllSessions"),

  getById: (sessionId) =>
  api.get(`/Session/GetSessionById/${sessionId}`),

  create: (payload) => api.post("/Session/CreateSession", payload),

  update: (sessionId, payload) => api.put(`/Session/UpdateSession/${sessionId}`, payload),

  delete: (sessionId) => api.delete(`/Session/DeleteSession/${sessionId}`),

  // Cover: carica il file al posto dell'URL. Risposta: { coverImgUrl }
  uploadCover: (sessionId, file) => postFile(`/Session/${sessionId}/Cover`, file),

  changeState: (sessionId, payload) =>
    api.patch(`/Session/${sessionId}/ChangeState`, payload),

  join: (sessionId, playerId) =>
    api.post(`/Session/${sessionId}/Join/${playerId}`),

  

  leave: (sessionId, playerId) =>
    api.post(`/Session/${sessionId}/Leave/${playerId}`),
};
