using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using CartaBranca.Mundo;

namespace CartaBranca.Nucleo
{
    /// <summary>Os tres momentos de uma partida.</summary>
    public enum EstadoDaPartida
    {
        Jogando,    // a mesa esta aberta (pode estar em pausa: ver GerenciadorDeJogo.Pausado)
        Morrendo,   // a Carta Branca caiu; camera lenta antes da tela de fim
        FimDeJogo   // tela de "mao morta" aberta, esperando o jogador
    }

    /// <summary>Cerebro da partida: ondas, pontuacao, fichas, sequencia, pausa,
    /// fim de jogo e o reinicio total ao estado inicial.
    /// Singleton simples acessado por GerenciadorDeJogo.Instancia.</summary>
    public class GerenciadorDeJogo : MonoBehaviour
    {
        public const string CENA_MENU = "Menu";

        public static GerenciadorDeJogo Instancia { get; private set; }

        [Header("Ritmo das ondas")]
        [SerializeField] float segundosPorOnda = 18f;
        [SerializeField] int   ondaMaxima = 30;
        [SerializeField] int   bonusPorOnda = 25;

        [Header("Morte")]
        [SerializeField] float pausaAposMorte = 1.1f;

        [Header("Recompensas")]
        [SerializeField] int   pontosPorFicha = 10;
        [SerializeField] float janelaSequencia = 2.6f;
        [SerializeField] int   abatesPorNivel = 3;
        [SerializeField] int   multiplicadorMaximo = 5;
        [SerializeField, Range(0f, 1f)] float taxaDaCasa = 0.25f;

        // Eventos: quem quiser reagir se inscreve, sem acoplamento.
        public event Action<int> AoTrocarOnda;
        public event Action AoReiniciar;
        public event Action AoMorrerJogador;
        public event Action AoFimDeJogo;
        public event Action<bool> AoPausar;
        public event Action<int> AoCasaCobrar;

        public EstadoDaPartida Estado { get; private set; }
        public bool Pausado { get; private set; }
        /// <summary>Verdadeiro sempre que a acao esta congelada (pausa, morte ou fim de jogo).</summary>
        public bool EmPausa { get { return Estado != EstadoDaPartida.Jogando || Pausado; } }

        public int Onda { get; private set; }
        public int Pontos { get; private set; }
        public int Abates { get; private set; }
        public float TempoDeVida { get; private set; }
        public string Mensagem { get; private set; }

        // ---- fichas: a moeda da mesa ----
        public int Fichas { get; private set; }
        public int FichasColetadas { get; private set; }
        public int FichasGastas { get; private set; }
        public int FichasPerdidas { get; private set; }
        public int PontosPorFicha { get { return pontosPorFicha; } }

        // ---- sequencia: abates encadeados multiplicam os pontos ----
        public int Sequencia { get; private set; }
        public int MelhorSequencia { get; private set; }
        public int Multiplicador { get { return Mathf.Min(1 + Sequencia / abatesPorNivel, multiplicadorMaximo); } }
        public float SequenciaRestante
        {
            get { return Sequencia > 0 ? Mathf.Clamp01((_fimSequencia - Time.time) / janelaSequencia) : 0f; }
        }

        /// <summary>O placar que vale recorde: pontos + fichas guardadas no bolso.</summary>
        public int PontuacaoFinal { get { return Pontos + Fichas * pontosPorFicha; } }
        public bool NovoRecorde { get; private set; }

        /// <summary>Multiplicador de dificuldade usado por inimigos e gerador.</summary>
        public float Escala { get { return Mathf.Min(2.4f, 1f + (Onda - 1) * 0.11f); } }

        float _proximaOnda;
        float _fimDaPausa;
        float _fimDaMensagem;
        float _fimSequencia;
        float _fimCamaraLenta;
        float _escalaLenta = 1f;
        bool _iniciado;
        readonly List<Inimigos.Inimigo> _vivos = new List<Inimigos.Inimigo>();

        public IReadOnlyList<Inimigos.Inimigo> Vivos { get { return _vivos; } }

        void Awake()
        {
            if (Instancia != null && Instancia != this)
            {
                Destroy(gameObject);
                return;
            }
            Instancia = this;
            Time.timeScale = 1f;
        }

        void OnDestroy()
        {
            if (Instancia == this)
            {
                Instancia = null;
                Time.timeScale = 1f;
            }
        }

        void Update()
        {
            // Roda depois de todos os Start(), garantindo que todo mundo ja se inscreveu nos eventos.
            if (!_iniciado)
            {
                _iniciado = true;
                PrepararPartida();
            }

            AtualizarEscalaDeTempo();
            if (!string.IsNullOrEmpty(Mensagem) && Time.unscaledTime > _fimDaMensagem) Mensagem = string.Empty;

            switch (Estado)
            {
                case EstadoDaPartida.Morrendo:
                    if (Time.time >= _fimDaPausa) EntrarNoFimDeJogo();
                    return;
                case EstadoDaPartida.FimDeJogo:
                    return;
            }

            if (Pausado) return;

            TempoDeVida += Time.deltaTime;

            if (Time.time >= _proximaOnda && Onda < ondaMaxima) AvancarOnda();
            if (Sequencia > 0 && Time.time > _fimSequencia) QuebrarSequencia(true);
        }

        void AtualizarEscalaDeTempo()
        {
            if (Pausado) Time.timeScale = 0f;
            else if (Time.unscaledTime < _fimCamaraLenta) Time.timeScale = _escalaLenta;
            else Time.timeScale = 1f;
        }

        void AvancarOnda()
        {
            Onda++;
            _proximaOnda = Time.time + segundosPorOnda;
            int bonus = bonusPorOnda * Onda;
            Pontos += bonus;
            MostrarMensagem("ONDA " + Onda + "  -  a casa aumenta a aposta   +" + bonus);
            Sonoplasta.Tocar(Som.Onda);
            if (AoTrocarOnda != null) AoTrocarOnda(Onda);
        }

        void PrepararPartida()
        {
            Estado = EstadoDaPartida.Jogando;
            Pausado = false;
            NovoRecorde = false;
            Onda = 1;
            Pontos = 0;
            Abates = 0;
            TempoDeVida = 0f;
            Fichas = 0;
            FichasColetadas = 0;
            FichasGastas = 0;
            FichasPerdidas = 0;
            Sequencia = 0;
            MelhorSequencia = 0;
            _fimCamaraLenta = 0f;
            _proximaOnda = Time.time + segundosPorOnda;
            _vivos.Clear();
            Time.timeScale = 1f;
            Sonoplasta.AbafarMusica(false);
            MostrarMensagem("EMBARALHA E DISTRIBUI");
            if (AoReiniciar != null) AoReiniciar();
        }

        // ------------------------------------------------------------ inimigos
        public void Registrar(Inimigos.Inimigo inimigo)
        {
            if (inimigo != null && !_vivos.Contains(inimigo)) _vivos.Add(inimigo);
        }

        public void Remover(Inimigos.Inimigo inimigo)
        {
            _vivos.Remove(inimigo);
        }

        /// <summary>Sobrecarga curta: abate comum, por carta.</summary>
        public void ContarAbate(Inimigos.Inimigo inimigo)
        {
            ContarAbate(inimigo, false);
        }

        /// <summary>Chamado pelo Inimigo quando ele e derrubado. Abates "pela mesa"
        /// (Ultima Mao) valem pontos, mas nao alimentam a sequencia.</summary>
        public void ContarAbate(Inimigos.Inimigo inimigo, bool pelaMesa)
        {
            if (inimigo == null) return;
            Abates++;

            if (!pelaMesa)
            {
                int antes = Multiplicador;
                Sequencia++;
                _fimSequencia = Time.time + janelaSequencia;
                MelhorSequencia = Mathf.Max(MelhorSequencia, Sequencia);
                if (Multiplicador > antes) MostrarMensagem("SEQUÊNCIA  x" + Multiplicador, 1.1f);
            }

            Pontos += Mathf.RoundToInt(inimigo.Pontos * Escala * (pelaMesa ? 1 : Multiplicador));
        }

        void QuebrarSequencia(bool avisar)
        {
            if (avisar && Sequencia >= abatesPorNivel) MostrarMensagem("sequência quebrada", 1f);
            Sequencia = 0;
        }

        // ------------------------------------------------------------ fichas
        public void ColetarFicha(int valor)
        {
            Fichas += valor;
            FichasColetadas += valor;
        }

        /// <summary>Tenta pagar um custo em fichas. Devolve false se o bolso nao cobre.</summary>
        public bool GastarFichas(int custo)
        {
            if (custo <= 0) return true;
            if (Fichas < custo) return false;
            Fichas -= custo;
            FichasGastas += custo;
            return true;
        }

        /// <summary>Punicao: o Naipe de Ouros escapou. A casa leva parte do bolso
        /// e a proxima onda chega na hora.</summary>
        public void OurosFugiu()
        {
            if (Estado != EstadoDaPartida.Jogando) return;
            int taxa = Mathf.CeilToInt(Fichas * taxaDaCasa);
            Fichas -= taxa;
            FichasPerdidas += taxa;
            if (Onda < ondaMaxima) _proximaOnda = Time.time + 0.6f;

            MostrarMensagem(taxa > 0
                ? "OUROS FUGIU  -  a casa cobra " + taxa + " fichas"
                : "OUROS FUGIU  -  a casa ri do seu bolso vazio", 2.4f);
            Sonoplasta.Tocar(Som.Cobra);
            CameraSuave.Tremer(0.35f);
            if (AoCasaCobrar != null) AoCasaCobrar(taxa);
        }

        // ------------------------------------------------------------ morte e fim
        /// <summary>Regra do desafio: ao ser atingido, o personagem morre. Depois da camera lenta
        /// abre a tela de fim; "jogar de novo" volta tudo ao estado inicial.</summary>
        public void MorteDoJogador()
        {
            if (Estado != EstadoDaPartida.Jogando) return;
            Estado = EstadoDaPartida.Morrendo;
            Pausado = false;
            _fimDaPausa = Time.time + pausaAposMorte;
            QuebrarSequencia(false);
            MostrarMensagem("MÃO MORTA", pausaAposMorte + 0.5f);
            if (AoMorrerJogador != null) AoMorrerJogador();
        }

        void EntrarNoFimDeJogo()
        {
            Estado = EstadoDaPartida.FimDeJogo;
            NovoRecorde = Recordes.Salvar(PontuacaoFinal, Onda, TempoDeVida);
            Mensagem = string.Empty;
            Sonoplasta.AbafarMusica(true);
            if (AoFimDeJogo != null) AoFimDeJogo();
        }

        // ------------------------------------------------------------ pausa e navegacao
        public void Pausar(bool pausar)
        {
            if (Estado != EstadoDaPartida.Jogando || Pausado == pausar) return;
            Pausado = pausar;
            AtualizarEscalaDeTempo();
            Sonoplasta.AbafarMusica(pausar);
            if (AoPausar != null) AoPausar(pausar);
        }

        public void AlternarPausa()
        {
            Pausar(!Pausado);
        }

        /// <summary>Volta a partida inteira ao estado inicial (regra do DIU2).</summary>
        public void Reiniciar()
        {
            bool estavaPausado = Pausado;
            PrepararPartida();
            if (estavaPausado && AoPausar != null) AoPausar(false);
        }

        public void IrParaMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(CENA_MENU);
        }

        /// <summary>Camera lenta por alguns segundos reais (Ultima Mao, morte).</summary>
        public void CamaraLenta(float duracaoReal, float escala)
        {
            _fimCamaraLenta = Time.unscaledTime + duracaoReal;
            _escalaLenta = Mathf.Clamp(escala, 0.05f, 1f);
        }

        public void MostrarMensagem(string texto)
        {
            MostrarMensagem(texto, 2.2f);
        }

        public void MostrarMensagem(string texto, float duracao)
        {
            Mensagem = texto;
            _fimDaMensagem = Time.unscaledTime + duracao;
        }
    }
}
