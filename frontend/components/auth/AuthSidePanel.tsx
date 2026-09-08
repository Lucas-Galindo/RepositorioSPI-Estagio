const pontos = [
  {
    label: "Agenda semanal e controle de aulas",
    path: (
      <>
        <rect x="3" y="4" width="18" height="18" rx="2.5" />
        <path d="M16 2v4M8 2v4M3 10h18" />
        <path d="m9 16 2 2 4-4" />
      </>
    ),
  },
  {
    label: "Pagamentos e financeiro centralizados",
    path: (
      <>
        <rect x="2" y="6" width="20" height="14" rx="2.5" />
        <path d="M2 10h20" />
        <path d="M6 15h4" />
      </>
    ),
  },
  {
    label: "Lembretes automáticos para os alunos",
    path: (
      <>
        <path d="M6 8a6 6 0 0 1 12 0c0 7 3 9 3 9H3s3-2 3-9" />
        <path d="M10.3 21a1.94 1.94 0 0 0 3.4 0" />
      </>
    ),
  },
];

export function AuthSidePanel() {
  return (
    <aside className="auth-side">
      <div className="side-brand">
        <div className="brand-mark">SPI</div>
        <div className="brand-text">
          <div className="name">SPI</div>
          <div className="sub">Professoras Independentes</div>
        </div>
      </div>

      <div className="side-copy">
        <h2>Sua rotina de ensino, organizada em um só lugar.</h2>
        <p>
          Gerencie alunos, turmas, aulas, agenda e pagamentos com a mesma facilidade de um app, mas
          com a organização de um sistema completo.
        </p>
      </div>

      <div className="side-points">
        {pontos.map((ponto) => (
          <div className="side-point" key={ponto.label}>
            <div className="ic">
              <svg
                width="15"
                height="15"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="2"
                strokeLinecap="round"
                strokeLinejoin="round"
              >
                {ponto.path}
              </svg>
            </div>
            <span>{ponto.label}</span>
          </div>
        ))}
      </div>

      <div className="side-quote">SPI — Sistema para Professoras Independentes</div>
    </aside>
  );
}
