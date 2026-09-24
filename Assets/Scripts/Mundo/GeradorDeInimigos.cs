using System.Collections.Generic;
using UnityEngine;
using CartaBranca.Nucleo;
using CartaBranca.Inimigos;

namespace CartaBranca.Mundo
{
    /// <summary>Distribui as cartas da casa: cria inimigos aos poucos, com aumento
    /// gradativo de quantidade, velocidade, vida e presenca de Copas (voadores) e Paus (IA de rota).
    /// Tambem cuida do respawn quando um inimigo e derrubado e do evento do Naipe de Ouros.</summary>
    public class GeradorDeInimigos : MonoBehaviour
    {
        [Header("Prefabs")]
        public GameObject espadasPrefab;
        public GameObject copasPrefab;
        public GameObject pausPrefab;
        public GameObject ourosPrefab;

        [Header("Pontos de entrada")]
        public List<Transform> pontosChao = new List<Transform>();
        public List<Transform> pontosAr   = new List<Transform>();

        [Header("Curva de dificuldade")]
        [SerializeField] int   simultaneosBase = 2;
        [SerializeField] int   simultaneosMax  = 12;
        [SerializeField] float intervaloBase   = 2.6f;
        [SerializeField] float intervaloMin    = 0.55f;
        [SerializeField] float atrasoRespawn   = 1.2f;
        [SerializeField] int   ondaDosVoadores = 2;
        [SerializeField] int   ondaDaVidaDupla = 5;
        [SerializeField] int   ondaDoPaus      = 3;

        [Header("Naipe de Ouros")]
        [SerializeField] int   ondaDoOuros     = 2;
        [SerializeField] float atrasoDoOuros   = 3f;

        readonly List<Inimigo> _ativos = new List<Inimigo>();
        float _proximo;
        float _ourosEm = -1f;
        InimigoOuros _ouros;

        // --- valores derivados da onda: a "curva" que o desafio pede ---
        int Onda { get { return GerenciadorDeJogo.Instancia != null ? GerenciadorDeJogo.Instancia.Onda : 1; } }
        float Escala { get { return GerenciadorDeJogo.Instancia != null ? GerenciadorDeJogo.Instancia.Escala : 1f; } }

        public int Simultaneos { get { return Mathf.Min(simultaneosBase + Onda, simultaneosMax); } }
        public float Intervalo { get { return Mathf.Max(intervaloMin, intervaloBase - (Onda - 1) * 0.16f); } }
        public float ChanceDeVoador
        {
            get { return Onda < ondaDosVoadores ? 0f : Mathf.Min(0.55f, 0.12f * (Onda - ondaDosVoadores + 1)); }
        }
        public float ChanceDePaus
        {
            get { return Onda < ondaDoPaus ? 0f : Mathf.Min(0.3f, 0.1f * (Onda - ondaDoPaus + 1)); }
        }
        public int VidaDoInimigo { get { return Onda >= ondaDaVidaDupla ? 1 + (Onda - ondaDaVidaDupla) / 5 : 1; } }

        public int Ativos { get { return _ativos.Count; } }
        public InimigoOuros OurosAtivo { get { return _ouros; } }

        void Start()
        {
            GerenciadorDeJogo jogo = GerenciadorDeJogo.Instancia;
            if (jogo == null) return;
            jogo.AoReiniciar += LimparTudo;
            jogo.AoTrocarOnda += AgendarOuros;
        }

        void OnDestroy()
        {
            GerenciadorDeJogo jogo = GerenciadorDeJogo.Instancia;
            if (jogo == null) return;
            jogo.AoReiniciar -= LimparTudo;
            jogo.AoTrocarOnda -= AgendarOuros;
        }

        void Update()
        {
            GerenciadorDeJogo jogo = GerenciadorDeJogo.Instancia;
            if (jogo == null || jogo.EmPausa) return;

            if (_ourosEm > 0f && Time.time >= _ourosEm)
            {
                _ourosEm = -1f;
                GerarOuros();
            }

            if (Time.time >= _proximo && _ativos.Count < Simultaneos)
            {
                Gerar();
                _proximo = Time.time + Intervalo;
            }
        }

        void Gerar()
        {
            float sorteio = Random.value;
            GameObject prefab;
            List<Transform> pontos;

            if (copasPrefab != null && sorteio < ChanceDeVoador)
            {
                prefab = copasPrefab;
                pontos = pontosAr;
            }
            else if (pausPrefab != null && sorteio < ChanceDeVoador + ChanceDePaus)
            {
                prefab = pausPrefab;
                pontos = pontosAr;
            }
            else
            {
                prefab = espadasPrefab;
                pontos = pontosChao;
            }

            Inimigo inimigo = Criar(prefab, pontos);
            if (inimigo == null) return;
            inimigo.AoSair += AoSairInimigo;
            _ativos.Add(inimigo);
        }

        Inimigo Criar(GameObject prefab, List<Transform> pontos)
        {
            if (prefab == null || pontos == null || pontos.Count == 0) return null;

            Transform ponto = pontos[Random.Range(0, pontos.Count)];
            GameObject go = Instantiate(prefab, ponto.position, Quaternion.identity);
            go.name = prefab.name + "_" + Time.frameCount;

            Inimigo inimigo = go.GetComponent<Inimigo>();
            if (inimigo != null) inimigo.Configurar(Escala, VidaDoInimigo);
            return inimigo;
        }

        void AgendarOuros(int onda)
        {
            if (onda >= ondaDoOuros && ourosPrefab != null) _ourosEm = Time.time + atrasoDoOuros;
        }

        void GerarOuros()
        {
            if (_ouros != null) return;
            Inimigo inimigo = Criar(ourosPrefab, pontosAr);
            _ouros = inimigo as InimigoOuros;   // cast dentro da hierarquia
            if (_ouros == null) return;

            _ouros.AoSair += quem => _ouros = null;
            if (GerenciadorDeJogo.Instancia != null)
                GerenciadorDeJogo.Instancia.MostrarMensagem("OUROS NA MESA  -  derrube antes que ele fuja com o pote", 2.6f);
            Sonoplasta.Tocar(Som.Ouros);
        }

        /// <summary>Regra do desafio: derrubou um inimigo, outro entra no lugar depois de um instante.
        /// Quanto maior a onda, menor a espera.</summary>
        void AoSairInimigo(Inimigo inimigo)
        {
            _ativos.Remove(inimigo);
            float espera = Mathf.Max(0.25f, atrasoRespawn - (Onda - 1) * 0.06f);
            _proximo = Mathf.Min(_proximo, Time.time + espera);
        }

        void LimparTudo()
        {
            for (int i = _ativos.Count - 1; i >= 0; i--)
            {
                if (_ativos[i] != null) _ativos[i].Retirar();
            }
            _ativos.Clear();
            if (_ouros != null) _ouros.Retirar();
            _ouros = null;
            _ourosEm = -1f;
            _proximo = Time.time + 1.1f;
        }
    }
}
