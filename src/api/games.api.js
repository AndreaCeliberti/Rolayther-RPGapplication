import api from "./axios";
import { postFile } from "./upload";

export const GamesApi = {
  getAll: () => api.get("/Game/GetAllGames"),

  create: (payload) =>
    api.post("/Game/createGame", payload),

  // Cover: carica il file al posto dell'URL. Risposta: { coverImageUrl }
  uploadCover: (gameId, file) => postFile(`/Game/${gameId}/Cover`, file),

  delete: (gameId) =>
    api.delete(`/Game/DeleteGame/${gameId}`),
};
