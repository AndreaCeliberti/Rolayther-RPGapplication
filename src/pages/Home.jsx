import { useContext, useMemo } from "react";
import { AuthContext } from "../context/AuthContext";
import { useHomeData } from "../hooks/useHomeData";
import { useMemberInfo } from "../hooks/useMemberInfo";
import { buildHomeView } from "../utils/homeSessions";
import HomeHero from "../components/home/HomeHero";
import Ornament from "../components/home/Ornament";
import SessionAgenda from "../components/home/SessionAgenda";
import HowItWorks from "../components/home/HowItWorks";
import MastersRow from "../components/home/MastersRow";
import GamesRow from "../components/home/GamesRow";
import JoinBand from "../components/home/JoinBand";
import "../styles/home.css";

const ALL_SESSIONS = { to: "/sessions", label: "Tutte le sessioni" };

// Ciò che l'agenda principale dice (titolo, spiegazione, testo quando è vuota) cambia con chi guarda la pagina
function agendaCopy({ role, hasMine }) {
  if (role === "Player") {
    return {
      title: "Tavoli con posti liberi",
      lead: "Scegli dove sederti: il disegno mostra quanti posti restano.",
      empty: {
        title: "Nessun tavolo con posti liberi al momento.",
        text: "Appena un Master pubblica una nuova sessione la trovi qui.",
      },
    };
  }
  if (role === "Master" || role === "Admin") {
    return {
      title: hasMine ? "Gli altri tavoli" : "Le prossime sessioni",
      lead: "Le sessioni in programma su Rolayther.",
      empty: {
        title: hasMine ? "Non ci sono altre sessioni in programma." : "Nessuna sessione in programma.",
        text: "Quando ne verranno pubblicate, le trovi qui.",
      },
    };
  }
  return {
    title: "Le prossime sessioni",
    lead: "Ogni riga è un tavolo: il disegno mostra quanti posti restano.",
    empty: {
      title: "Nessuna sessione in programma.",
      text: "Quando un Master ne pubblica una, la trovi qui.",
    },
  };
}

export default function Home({ openLogin, openRegisterPlayer, openRegisterMaster, openCreateSession }) {
  const { user } = useContext(AuthContext);
  const { data, retry, waking } = useHomeData();
  const member = useMemberInfo(user);
  const view = useMemo(() => buildHomeView(data, user, member), [data, user, member]);

  const role = user?.role ?? null;
  const sessionsStatus = data.sessions.status;

  // "Quante sessioni ho" si sa solo quando sono arrivate le sessioni (e, per il Master, il suo id)
  const waitingForMaster = role === "Master" && !member.ready;
  const mineLoading = sessionsStatus === "loading" || waitingForMaster;
  const mineKnown = sessionsStatus === "ready" && !waitingForMaster && (role !== "Master" || !!member.masterId);
  const hasMine = view.mine.length > 0;

  const copy = agendaCopy({ role, hasMine });
  const mineIds = new Set(view.mine.map((s) => s.id));
  const mainSessions = role === "Player" ? view.openTables : view.upcoming.filter((s) => !mineIds.has(s.id));

  // Se la lista è vuota, a chi può aprire un tavolo si offre di farlo
  let emptyAction = null;
  if (!role) emptyAction = { label: "Sei un Master? Apri il primo tavolo", onClick: openRegisterMaster };
  if (role === "Master") emptyAction = { label: "Crea una sessione", onClick: openCreateSession };

  return (
    <div className="home">
      <HomeHero
        role={role}
        name={member.nickName}
        status={sessionsStatus}
        featured={view.featured}
        featuredIsMine={view.featuredIsMine}
        sessionsCount={view.upcoming.length}
        hostCount={view.hostCount}
        mineCount={mineKnown ? view.mine.length : null}
        mineLoading={mineLoading}
        pendingCount={member.pendingCount}
        onRegisterPlayer={openRegisterPlayer}
        onRegisterMaster={openRegisterMaster}
        onCreateSession={openCreateSession}
        onRetry={retry}
      />

      <Ornament />

      {hasMine && (
        <SessionAgenda
          headingId="hm-mine-title"
          title="Le tue sessioni"
          lead={role === "Master" ? "I tavoli che dirigi." : "I tavoli in cui hai un posto."}
          status="ready"
          sessions={view.mine}
          limit={6}
        />
      )}

      <SessionAgenda
        id="sessioni"
        headingId="hm-agenda-title"
        title={copy.title}
        lead={copy.lead}
        status={sessionsStatus}
        sessions={mainSessions}
        limit={6}
        action={ALL_SESSIONS}
        empty={{ ...copy.empty, action: emptyAction }}
        waking={waking}
        onRetry={retry}
      />

      {!role && <HowItWorks />}

      <MastersRow masters={view.masters} />
      <GamesRow games={view.games} genres={view.genres} />

      {!role && <JoinBand onRegisterPlayer={openRegisterPlayer} onRegisterMaster={openRegisterMaster} onLogin={openLogin} />}
    </div>
  );
}
