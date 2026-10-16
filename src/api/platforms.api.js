import api from "./axios";
import { postFile } from "./upload";

export const PlatformsApi = {
  getAll: () => api.get("/Platform/GetAllPlatforms"),

  create: (payload) =>
    api.post("/Platform/CreatePlatform", payload),

  // Logo: carica il file al posto dell'URL. Risposta: { logoUrl }
  uploadLogo: (platformId, file) => postFile(`/Platform/${platformId}/Logo`, file),

  delete: (platformId) =>
    api.delete(`/Platform/DeletePlatform/${platformId}`),
};
