using System.Collections.Generic;
using UnityEngine;
using CartaBranca.Nucleo;
using CartaBranca.Inimigos;

namespace CartaBranca.Mundo
{
    /// <summary>Distribui as cartas da casa: cria inimigos aos poucos, com aumento
    /// gradativo de quantidade, velocidade, vida e presenca de Copas (voadores).
    /// Tambem cuida do respawn quando um inimigo e derrubado.</summary>
    public class GeradorDeInimigos : MonoBehaviour
    {
        [Header("Prefabs")]
        public GameObject espadasPrefab;
        public GameObject copasPrefab;

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

        readonly List<Inimigo> _ativos = new List<Inimigo>();
        float _proximo;

        // --- valores derivados da onda: a "curva" que o desafio pede ---
        int Onda { get { return GerenciadorDeJogo.Instancia != null ? GerenciadorDeJogo.Instancia.Onda : 1; } }
        float Escala { get { return GerenciadorDeJogo.Instancia != null ? GerenciadorDeJogo.Instancia.Escala : 1f; } }

        public int Simultaneos { get { return Mathf.Min(simultaneosBase + Onda, simultaneosMax); } }
        public float Intervalo { get { return Mathf.Max(intervaloMin, intervaloBase - (Onda - 1) * 0.16f); } }
        public float ChanceDeVoador
        {
            get { return Onda < ondaDosVoadores ? 0f : Mathf.Min(0.55f, 0.12f * (Onda - ondaDosVoadores + 1)); }
        }
        public int VidaDoInimigo { get { return Onda >= ondaDaVidaDupla ? 1 + (Onda - ondaDaVidaDupla) / 5 : 1; } }

        void Start()
        {
            if (GerenciadorDeJogo.Instancia != null)
                GerenciadorDeJogo.Instancia.AoReiniciar += LimparTudo;
        }

        void OnDestroy()
        {
            if (GerenciadorDeJogo.Instancia != null)
                GerenciadorDeJogo.Instancia.AoReiniciar -= LimparTudo;
        }

        void Update()
        {
            GerenciadorDeJogo jogo = GerenciadorDeJogo.Instancia;
            if (jogo == null || jogo.EmPausa) return;

            if (Time.time >= _proximo && _ativos.Count < Simultaneos)
            {
                Gerar();
                _proximo = Time.time + Intervalo;
            }
        }

        void Gerar()
        {
            bool voador = copasPrefab != null && Random.value < ChanceDeVoador;
            GameObject prefab = voador ? copasPrefab : espadasPrefab;
            List<Transform> pontos = voador ? pontosAr : pontosChao;
            if (prefab == null || pontos == null || pontos.Count == 0) return;

            Transform ponto = pontos[Random.Range(0, pontos.Count)];
            GameObject go = Instantiate(prefab, ponto.position, Quaternion.identity);
            go.name = prefab.name + "_" + Time.frameCount;

            Inimigo inimigo = go.GetComponent<Inimigo>();
            if (inimigo == null) return;

            inimigo.Configurar(Escala, VidaDoInimigo);
            inimigo.AoSair += AoSairInimigo;
            _ativos.Add(inimigo);
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
            _proximo = Time.time + 1.1f;
        }

        public int Ativos { get { return _ativos.Count; } }
    }
}
