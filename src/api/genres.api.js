import api from "./axios";
import { postFile } from "./upload";

export const GenresApi = {
  getAll: () => api.get("/Genre/GetAllGenres"),

  create: (payload) =>
    api.post("/Genre/CreateGenre", payload),

  // Immagine: carica il file al posto dell'URL. Risposta: { imageUrl }
  uploadImage: (genreId, file) => postFile(`/Genre/${genreId}/Image`, file),

  delete: (genreId) =>
    api.delete(`/Genre/DeleteGenre/${genreId}`),
};
