import { createContext, useState } from "react";
import { Toast, ToastContainer } from "react-bootstrap";

export const ToastContext = createContext();

export function ToastProvider({ children }) {
  const [toast, setToast] = useState({
    show: false,
    message: "",
    variant: "success",
    delay: 3000,
  });

  // delay (ms) = quanto resta visibile. Se non lo si indica dipende dalla lunghezza del messaggio
  // (da 3 a 10 secondi), così le frasi lunghe, come "controlla la tua email", si fanno leggere.
  const showToast = (message, variant = "success", delay) => {
    const automatic = Math.min(10000, Math.max(3000, String(message ?? "").length * 60));
    setToast({ show: true, message, variant, delay: delay ?? automatic });
  };

  return (
    <ToastContext.Provider value={{ showToast }}>
      {children}

      <ToastContainer position="top-end" className="p-3">
        <Toast
          bg={toast.variant}
          onClose={() => setToast({ ...toast, show: false })}
          show={toast.show}
          delay={toast.delay}
          autohide
        >
          <Toast.Body className="text-white">{toast.message}</Toast.Body>
        </Toast>
      </ToastContainer>
    </ToastContext.Provider>
  );
}
