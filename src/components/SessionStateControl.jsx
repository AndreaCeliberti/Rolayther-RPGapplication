import { useContext, useMemo, useState } from "react";
import { Button, Form, InputGroup, Spinner } from "react-bootstrap";
import { ToastContext } from "../context/ToastContext";
import { SessionsApi } from "../api/sessions.api";
import { stateLabel, STATE_LABELS } from "../utils/sessionState";
import { StateBadge } from "./common/Badges";
import { allowedNextStates } from "../utils/sessionState";


export default function SessionStateControl({ session, onUpdated }) {
  const { showToast } = useContext(ToastContext);

  const initialState = session?.currentState ?? session?.CurrentState ?? 0;

  const [newState, setNewState] = useState(initialState);
  const [reason, setReason] = useState("");
  const [saving, setSaving] = useState(false);

  const current = useMemo(() => Number(initialState), [initialState]);
  const selected = useMemo(() => Number(newState), [newState]);
  const changed = selected !== current;
  const nextStates = allowedNextStates(current);
  const options = [current, ...nextStates];


  const handleSave = async () => {
    if (!changed) return;

    setSaving(true);
    try {
      await SessionsApi.changeState(session.sessionId, {
        newState: selected,
        reason: reason?.trim() ? reason.trim() : null,
      });

      showToast(`Stato aggiornato: ${stateLabel(selected)} `, "success");
      setReason("");
      onUpdated?.();
    } catch (err) {
      const msg =
        err?.response?.data?.message ||
        err?.response?.data?.Message ||
        "Errore durante il cambio stato";
      showToast(msg, "danger");
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="d-flex flex-column gap-2">
      <div className="d-flex align-items-center justify-content-between">
        <div className="text-muted small">Stato attuale</div>
        <StateBadge state={current} />
      </div>

      <InputGroup>
        <Form.Select
        value={selected}
        onChange={(e) => setNewState(Number(e.target.value))}
        >
        {options.map((st) => (
            <option key={st} value={st}>
            {stateLabel(st)}
            </option>
        ))}
        </Form.Select>


        <Button
          variant={changed ? "primary" : "outline-secondary"}
          disabled={!changed || saving}
          onClick={handleSave}
        >
          {saving ? (
            <>
              <Spinner size="sm" className="me-2" />
              Salvo…
            </>
          ) : (
            "Salva"
          )}
        </Button>
      </InputGroup>

      
      
    </div>
  );
}
