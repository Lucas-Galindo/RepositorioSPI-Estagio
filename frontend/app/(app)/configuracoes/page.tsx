"use client";

import { Icon } from "@/components/shared/Icon";
import { useTheme, type ColorMode } from "@/contexts/ThemeContext";
import { useToast } from "@/contexts/ToastContext";
import { usePageHeader } from "@/lib/usePageHeader";

const COLOR_MODES: { id: ColorMode; nome: string; desc: string; swatches: string[] }[] = [
  { id: "normal", nome: "Padrão", desc: "Paleta original do SPI.", swatches: ["#4ecdc4", "#bb9457", "#ffe66d", "#c0392b"] },
  { id: "protanopia", nome: "Protanopia", desc: "Ajuste para dificuldade com tons de vermelho.", swatches: ["#0072b2", "#e69f00", "#f0e442", "#d55e00"] },
  { id: "deuteranopia", nome: "Deuteranopia", desc: "Ajuste para dificuldade com tons de verde.", swatches: ["#0072b2", "#cc79a7", "#f0e442", "#d55e00"] },
  { id: "tritanopia", nome: "Tritanopia", desc: "Ajuste para dificuldade com tons de azul.", swatches: ["#cc79a7", "#e69f00", "#009e73", "#d55e00"] },
];

export default function ConfiguracoesPage() {
  usePageHeader("Configurações", "Aparência e acessibilidade do sistema");
  const { dark, setDark, colorMode, setColorMode, accentColor, setAccentColor, resetAccentColor } = useTheme();
  const { mostrarToast } = useToast();

  return (
    <>
      <div className="settings-card">
        <div className="settings-head">
          <div className="ic">
            <Icon name="sun" size={18} />
          </div>
          <div>
            <h3>Tema</h3>
            <p>Escolha entre o modo claro e escuro.</p>
          </div>
        </div>
        <div className="theme-preview">
          <button type="button" className={`theme-opt light${!dark ? " active" : ""}`} onClick={() => setDark(false)}>
            <div className="ic">
              <Icon name="sun" size={17} />
            </div>
            <div className="lb">Modo claro</div>
          </button>
          <button type="button" className={`theme-opt dark${dark ? " active" : ""}`} onClick={() => setDark(true)}>
            <div className="ic">
              <Icon name="moon" size={17} />
            </div>
            <div className="lb">Modo escuro</div>
          </button>
        </div>
      </div>

      <div className="settings-card">
        <div className="settings-head">
          <div className="ic">
            <Icon name="shield" size={18} />
          </div>
          <div>
            <h3>Modo de cores</h3>
            <p>Paletas seguras para diferentes tipos de daltonismo.</p>
          </div>
        </div>
        <div className="colormode-grid">
          {COLOR_MODES.map((cm) => (
            <button
              type="button"
              key={cm.id}
              className={`colormode-card${colorMode === cm.id ? " active" : ""}`}
              onClick={() => {
                setColorMode(cm.id);
                mostrarToast(`Modo de cores atualizado: ${cm.nome}.`);
              }}
            >
              <div className="colormode-swatches">
                {cm.swatches.map((cor) => (
                  <span className="swatch" style={{ background: cor }} key={cor} />
                ))}
              </div>
              <div className="cm-name">{cm.nome}</div>
              <div className="cm-desc">{cm.desc}</div>
            </button>
          ))}
        </div>
      </div>

      <div className="settings-card">
        <div className="settings-head">
          <div className="ic">
            <Icon name="palette" size={18} />
          </div>
          <div>
            <h3>Cor de destaque</h3>
            <p>Personalize a cor principal do sistema (só no modo de cores Padrão).</p>
          </div>
        </div>
        <div className="accent-picker-row">
          <label className="accent-swatch-label">
            <input
              type="color"
              value={accentColor}
              disabled={colorMode !== "normal"}
              onChange={(e) => setAccentColor(e.target.value)}
            />
            <span>Escolher cor</span>
          </label>
          <div className="accent-picker-info">
            <div className="accent-current">
              Cor atual: <b>{accentColor}</b>
            </div>
            <div className="accent-hint">
              {colorMode !== "normal"
                ? "Desativada enquanto um modo de daltonismo estiver ativo, para não sobrepor a paleta de acessibilidade."
                : "A cor escolhida se aplica a botões, links e destaques em todo o sistema."}
            </div>
            <button
              type="button"
              className="btn btn-ghost btn-sm"
              style={{ marginTop: 10 }}
              onClick={() => {
                resetAccentColor();
                mostrarToast("Cor de destaque restaurada para o padrão.");
              }}
            >
              Restaurar padrão
            </button>
          </div>
        </div>
      </div>
    </>
  );
}
