import { useContext, useEffect } from "react";
import { useSearchParams } from "react-router-dom";
import { AuthContext } from "../../context/AuthContext";

// I pulsanti "Accedi a Rolayther" delle email puntano a  /?login=1 :
// questo componente (invisibile) apre la finestra di login, se serve, e toglie il parametro dall'indirizzo.
// Chi è già collegato non vede nulla.
export default function LoginLinkHandler({ openLogin }) {
  const { user } = useContext(AuthContext);
  const [params, setParams] = useSearchParams();
  const wantsLogin = params.get("login") === "1";

  useEffect(() => {
    if (!wantsLogin) return;

    if (!user) openLogin();

    setParams(
      (current) => {
        const next = new URLSearchParams(current);
        next.delete("login");
        return next;
      },
      { replace: true }
    );
  }, [wantsLogin, user, openLogin, setParams]);

  return null;
}
