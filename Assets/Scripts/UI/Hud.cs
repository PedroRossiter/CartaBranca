using UnityEngine;
using UnityEngine.UI;
using CartaBranca.Nucleo;
using CartaBranca.Jogador;
using CartaBranca.Inimigos;
using CartaBranca.Mundo;

namespace CartaBranca.UI
{
    /// <summary>Placar da mesa: onda, pontos, tempo, recorde, aviso central, recarga da esquiva
    /// e, no DIU3, fichas, sequencia, custo da Ultima Mao e o alerta do Naipe de Ouros.</summary>
    public class Hud : MonoBehaviour
    {
        public Text onda;
        public Text pontos;
        public Text tempo;
        public Text recorde;
        public Text aviso;
        public Text ajuda;
        public Image barraEsquiva;

        [Header("DIU3")]
        public Text fichas;
        public Text sequencia;
        public Image barraSequencia;
        public Text especial;
        public Text alertaOuros;
        public Text mao;   // cartas na mao / embaralhando

        static readonly Color CARMIM = new Color(0.87f, 0.27f, 0.36f);
        static readonly Color OURO   = new Color(0.78f, 0.64f, 0.35f);
        static readonly Color CINZA  = new Color(0.45f, 0.52f, 0.66f);
        static readonly Color PAPEL  = new Color(0.91f, 0.9f, 0.86f);

        ControleCartaBranca _jogador;
        UltimaMao _ultimaMao;
        GeradorDeInimigos _gerador;
        int _fichasAntes;
        float _pulso;

        void Start()
        {
            _jogador = Object.FindAnyObjectByType<ControleCartaBranca>();
            if (_jogador != null) _ultimaMao = _jogador.GetComponent<UltimaMao>();
            _gerador = Object.FindAnyObjectByType<GeradorDeInimigos>();
            if (ajuda != null)
                ajuda.text = "A/D mover   ESPAÇO pular   J carta   K esquiva   L última mão   ESC pausa   F2 rotas da IA";

            // cenas montadas antes da mao de cartas: cria o texto clonando o da Ultima Mao
            if (mao == null && especial != null)
            {
                mao = Instantiate(especial.gameObject, especial.transform.parent).GetComponent<Text>();
                mao.name = "Mao";
                RectTransform rt = mao.GetComponent<RectTransform>();
                rt.anchoredPosition = especial.GetComponent<RectTransform>().anchoredPosition + new Vector2(0f, 44f);
            }
        }

        void Update()
        {
            GerenciadorDeJogo jogo = GerenciadorDeJogo.Instancia;
            if (jogo == null) return;

            if (Input.GetKeyDown(KeyCode.F2))
            {
                InimigoNavegante.MostrarRotas = !InimigoNavegante.MostrarRotas;
                jogo.MostrarMensagem(InimigoNavegante.MostrarRotas ? "ROTAS DA IA VISÍVEIS  (F2)" : "ROTAS DA IA OCULTAS  (F2)", 1.2f);
            }

            if (onda != null)    onda.text = "ONDA " + jogo.Onda;
            if (pontos != null)  pontos.text = jogo.Pontos + " PTS   " + jogo.Abates + " ABATES";
            if (tempo != null)   tempo.text = Formatar(jogo.TempoDeVida);
            if (recorde != null) recorde.text = "RECORDE " + Recordes.MelhorPontuacao;
            if (aviso != null)   aviso.text = jogo.Mensagem;

            if (barraEsquiva != null && _jogador != null)
            {
                barraEsquiva.fillAmount = _jogador.RecargaEsquiva;
                barraEsquiva.color = _jogador.RecargaEsquiva >= 1f ? CARMIM : CINZA;
            }

            AtualizarFichas(jogo);
            AtualizarSequencia(jogo);
            AtualizarEspecial(jogo);
            AtualizarMao();
            AtualizarOuros(jogo);
        }

        void AtualizarFichas(GerenciadorDeJogo jogo)
        {
            if (fichas == null) return;
            if (jogo.Fichas != _fichasAntes)
            {
                _pulso = 1f;
                _fichasAntes = jogo.Fichas;
            }
            _pulso = Mathf.MoveTowards(_pulso, 0f, Time.unscaledDeltaTime * 4f);
            fichas.text = "FICHAS " + jogo.Fichas + "   (vale " + jogo.Fichas * jogo.PontosPorFicha + " no fim)";
            fichas.transform.localScale = Vector3.one * (1f + 0.15f * _pulso);
        }

        void AtualizarSequencia(GerenciadorDeJogo jogo)
        {
            if (sequencia != null)
            {
                if (jogo.Sequencia > 0)
                {
                    sequencia.text = (jogo.Multiplicador > 1 ? "x" + jogo.Multiplicador + "   " : "") + "SEQUÊNCIA " + jogo.Sequencia;
                    sequencia.color = jogo.Multiplicador > 1 ? CARMIM : PAPEL;
                }
                else sequencia.text = string.Empty;
            }
            if (barraSequencia != null)
            {
                barraSequencia.fillAmount = jogo.SequenciaRestante;
                barraSequencia.color = jogo.Multiplicador > 1 ? CARMIM : PAPEL;
            }
        }

        void AtualizarEspecial(GerenciadorDeJogo jogo)
        {
            if (especial == null || _ultimaMao == null) return;
            especial.text = "[L] ÚLTIMA MÃO  ·  " + _ultimaMao.Custo + " FICHAS";
            especial.color = _ultimaMao.PodeUsar ? CARMIM : new Color(CINZA.r, CINZA.g, CINZA.b, 0.75f);
        }

        void AtualizarMao()
        {
            if (mao == null || _jogador == null) return;
            if (_jogador.Embaralhando)
            {
                mao.text = "EMBARALHANDO...  " + _jogador.TempoParaEmbaralhar.ToString("0.0") + "s";
                mao.color = new Color(CINZA.r, CINZA.g, CINZA.b, 0.6f + 0.4f * Mathf.PingPong(Time.time * 3f, 1f));
            }
            else
            {
                // uma barra por carta: cheia = na mao, vazia = ja jogada
                string cartas = "";
                for (int i = 0; i < _jogador.CartasMaximas; i++) cartas += i < _jogador.Cartas ? "I " : ". ";
                mao.text = "[J] CARTAS  " + cartas;
                mao.color = PAPEL;
            }
        }

        void AtualizarOuros(GerenciadorDeJogo jogo)
        {
            if (alertaOuros == null) return;
            InimigoOuros ouros = _gerador != null ? _gerador.OurosAtivo : null;
            if (ouros == null || ouros.Fugindo || jogo.EmPausa)
            {
                alertaOuros.text = string.Empty;
                return;
            }

            // seta indicando o lado quando o Ouros esta fora da tela
            string esquerda = "", direita = "";
            Camera cam = Camera.main;
            if (cam != null)
            {
                float dx = ouros.transform.position.x - cam.transform.position.x;
                float meia = cam.orthographicSize * cam.aspect;
                if (dx > meia) direita = "   >>>";
                else if (dx < -meia) esquerda = "<<<   ";
            }

            alertaOuros.text = esquerda + "OUROS NA MESA  -  FOGE EM " + ouros.TempoRestante.ToString("0.0") + "s" + direita;
            float a = ouros.TempoRestante < 3f ? 0.45f + 0.55f * Mathf.PingPong(Time.time * 4f, 1f) : 1f;
            alertaOuros.color = new Color(OURO.r, OURO.g, OURO.b, a);
        }

        public static string Formatar(float segundos)
        {
            int m = Mathf.FloorToInt(segundos / 60f);
            int s = Mathf.FloorToInt(segundos % 60f);
            return string.Format("{0:00}:{1:00}", m, s);
        }
    }
}
