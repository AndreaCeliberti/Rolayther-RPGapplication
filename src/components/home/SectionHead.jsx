import { Link } from "react-router-dom";

// Intestazione di una sezione della Home: titolo, una riga di spiegazione e, se serve, il collegamento alla pagina completa
export default function SectionHead({ id, title, lead, action }) {
  return (
    <header className="hm-head">
      <h2 id={id} className="hm-head__title">
        {title}
      </h2>
      {lead && <p className="hm-head__lead">{lead}</p>}
      {action && (
        <Link className="hm-link" to={action.to}>
          {action.label}
        </Link>
      )}
    </header>
  );
}
