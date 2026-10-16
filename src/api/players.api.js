import api from "./axios";
import { postFile } from "./upload";

export const PlayersApi = {
  getAll: () => api.get("/Player/GetAllPlayers"),

  getById: (playerId) =>
    api.get(`/Player/GetPlayer/${playerId}`),
  
  getMe: () => api.get("/Player/Me"),

  getSessions: (playerId) => api.get(`/Player/${playerId}/Sessions`),

  create: (payload) =>
    api.post("/Player/CreatePlayer", payload),

  update: (playerId, payload) =>
    api.put(`/Player/UpdatePlayer/${playerId}`, payload),

  // Foto profilo: carica il file (JPG, PNG, WebP, GIF) al posto dell'URL. Risposta: { avatarImgUrl }
  uploadAvatar: (playerId, file) => postFile(`/Player/${playerId}/Avatar`, file),

  delete: (playerId) =>
    api.delete(`/Player/DeletePlayer/${playerId}`),
};
