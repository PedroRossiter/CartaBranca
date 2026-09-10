using System;
using System.Collections.Generic;
using UnityEngine;

namespace CartaBranca.Nucleo
{
    /// <summary>Cerebro da partida: ondas, pontuacao, tempo de sobrevivencia
    /// e o reinicio total quando a Carta Branca morre.
    /// Singleton simples acessado por GerenciadorDeJogo.Instancia.</summary>
    public class GerenciadorDeJogo : MonoBehaviour
    {
        public static GerenciadorDeJogo Instancia { get; private set; }

        [Header("Ritmo das ondas")]
        [SerializeField] float segundosPorOnda = 18f;
        [SerializeField] int   ondaMaxima = 30;

        [Header("Reinicio")]
        [SerializeField] float pausaAposMorte = 1.1f;

        // Eventos: quem quiser reagir se inscreve, sem acoplamento.
        public event Action<int> AoTrocarOnda;
        public event Action AoReiniciar;
        public event Action AoMorrerJogador;

        public int Onda { get; private set; }
        public int Pontos { get; private set; }
        public int Abates { get; private set; }
        public float TempoDeVida { get; private set; }
        public bool EmPausa { get; private set; }
        public string Mensagem { get; private set; }

        /// <summary>Multiplicador de dificuldade usado por inimigos e gerador.</summary>
        public float Escala { get { return 1f + (Onda - 1) * 0.11f; } }

        float _proximaOnda;
        float _fimDaPausa;
        float _fimDaMensagem;
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
        }

        bool _iniciado;

        void Update()
        {
            // Roda depois de todos os Start(), garantindo que todo mundo ja se inscreveu nos eventos.
            if (!_iniciado)
            {
                _iniciado = true;
                PrepararPartida();
            }

            if (EmPausa)
            {
                if (Time.time >= _fimDaPausa) PrepararPartida();
                return;
            }

            TempoDeVida += Time.deltaTime;

            if (Time.time >= _proximaOnda && Onda < ondaMaxima)
            {
                Onda++;
                _proximaOnda = Time.time + segundosPorOnda;
                MostrarMensagem("ONDA " + Onda + "  -  a casa aumenta a aposta");
                if (AoTrocarOnda != null) AoTrocarOnda(Onda);
            }

            if (Time.time > _fimDaMensagem) Mensagem = string.Empty;
        }

        void PrepararPartida()
        {
            EmPausa = false;
            Onda = 1;
            Pontos = 0;
            Abates = 0;
            TempoDeVida = 0f;
            _proximaOnda = Time.time + segundosPorOnda;
            _vivos.Clear();
            MostrarMensagem("EMBARALHA E DISTRIBUI");
            if (AoReiniciar != null) AoReiniciar();
        }

        public void Registrar(Inimigos.Inimigo inimigo)
        {
            if (inimigo != null && !_vivos.Contains(inimigo)) _vivos.Add(inimigo);
        }

        public void Remover(Inimigos.Inimigo inimigo)
        {
            _vivos.Remove(inimigo);
        }

        /// <summary>Chamado pelo Inimigo quando ele e derrubado por uma carta.</summary>
        public void ContarAbate(Inimigos.Inimigo inimigo)
        {
            if (inimigo == null) return;
            Abates++;
            Pontos += Mathf.RoundToInt(inimigo.Pontos * Escala);
        }

        /// <summary>Regra do desafio: ao ser atingido, o personagem morre e o jogo
        /// volta ao estado inicial (onda 1, placar zerado, arena limpa).</summary>
        public void MorteDoJogador()
        {
            if (EmPausa) return;
            EmPausa = true;
            _fimDaPausa = Time.time + pausaAposMorte;

            bool recorde = Recordes.Salvar(Pontos, Onda);
            MostrarMensagem(recorde
                ? "MAO MORTA  -  novo recorde: " + Pontos
                : "MAO MORTA  -  " + Pontos + " pts na onda " + Onda);

            if (AoMorrerJogador != null) AoMorrerJogador();
        }

        public void MostrarMensagem(string texto, float duracao = 2.2f)
        {
            Mensagem = texto;
            _fimDaMensagem = Time.time + duracao;
        }

        void OnDestroy()
        {
            if (Instancia == this) Instancia = null;
        }
    }
}
