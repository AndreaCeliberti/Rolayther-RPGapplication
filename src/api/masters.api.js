import api from "./axios";

export const MastersApi = {
  getAll: () => api.get("/Master/GetAllMasters"),

  getById: (masterId) =>
    api.get(`/Master/GetMaster/${masterId}`),

  getMe: () => api.get("/Master/Me"),

  getMySessions: () => api.get("/Master/Me/Sessions"),
  
  getSessions: (masterId) => api.get(`/Master/${masterId}/Sessions`),

  create: (payload) =>
    api.post("/Master/CreateMaster", payload),

  update: (masterId, payload) =>
    api.put(`/Master/UpdateMaster/${masterId}`, payload),

  delete: (masterId) =>
    api.delete(`/Master/DeleteMaster/${masterId}`),

  getPending: () => api.get("/Master/PendingRequests"),

  approve: (masterId) => api.put(`/Master/${masterId}/Approve`),

  reject: (masterId, reason) =>
    api.put(`/Master/${masterId}/Reject`, { reason }),
};
