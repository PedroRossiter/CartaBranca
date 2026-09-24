using System.Collections.Generic;
using UnityEngine;
using CartaBranca.Nucleo;
using CartaBranca.Mundo;

namespace CartaBranca.Inimigos
{
    /// <summary>Base abstrata dos naipes que PENSAM o caminho (comportamento inteligente do DIU3).
    /// De tempos em tempos pede ao MapaDeNavegacao uma rota A* ate o Destino() e a segue,
    /// cortando caminho sempre que ha linha reta livre ("puxar o barbante").
    /// A subclasse so decide PARA ONDE ir: Paus persegue, Ouros foge.</summary>
    public abstract class InimigoNavegante : Inimigo
    {
        [Header("Navegacao")]
        [SerializeField] protected float intervaloReplanejar = 0.35f;

        /// <summary>Liga/desliga o desenho das rotas (tecla F2). Util para mostrar a IA no video.</summary>
        public static bool MostrarRotas;

        protected readonly List<Vector2> Rota = new List<Vector2>();
        int _indice;
        float _proximoPlano;
        LineRenderer _linha;

        /// <summary>Para onde este naipe quer ir agora.</summary>
        protected abstract Vector2 Destino();

        /// <summary>Velocidade usada para seguir a rota (Paus acelera no bote).</summary>
        protected virtual float VelocidadeAtual()
        {
            return Velocidade;
        }

        protected void ReplanejarJa()
        {
            _proximoPlano = 0f;
        }

        protected override void Comportamento()
        {
            if (Time.time >= _proximoPlano) Replanejar();
            Seguir();
            DesenharRota();
        }

        void Replanejar()
        {
            _proximoPlano = Time.time + intervaloReplanejar * Random.Range(0.85f, 1.15f);
            Vector2 destino = Destino();
            MapaDeNavegacao mapa = MapaDeNavegacao.Instancia;
            if (mapa == null || !mapa.Caminho(transform.position, destino, Rota))
            {
                Rota.Clear();
                Rota.Add(destino);   // sem mapa ou sem rota: vai reto
            }
            _indice = 0;
        }

        void Seguir()
        {
            if (Rota.Count == 0) return;
            Vector2 pos = transform.position;
            MapaDeNavegacao mapa = MapaDeNavegacao.Instancia;

            // pula os nos que ja estao em linha reta visivel
            while (_indice < Rota.Count - 1 && mapa != null && mapa.LinhaLivre(pos, Rota[_indice + 1])) _indice++;

            Vector2 alvo = Rota[_indice];
            Vector2 novo = Vector2.MoveTowards(pos, alvo, VelocidadeAtual() * Time.deltaTime);
            if ((novo - alvo).sqrMagnitude < 0.0025f && _indice < Rota.Count - 1) _indice++;
            transform.position = new Vector3(novo.x, novo.y, 0f);

            float dx = alvo.x - pos.x;
            if (Mathf.Abs(dx) > 0.05f)
            {
                Vector3 e = transform.localScale;
                e.x = Mathf.Abs(e.x) * (dx > 0f ? 1f : -1f);
                transform.localScale = e;
            }
        }

        void DesenharRota()
        {
            if (!MostrarRotas)
            {
                if (_linha != null) _linha.enabled = false;
                return;
            }

            if (_linha == null)
            {
                _linha = gameObject.AddComponent<LineRenderer>();
                _linha.useWorldSpace = true;
                _linha.widthMultiplier = 0.08f;
                _linha.material = new Material(Shader.Find("Sprites/Default"));
                _linha.sortingOrder = 55;
                Color c = naipe.Vermelho() ? new Color(0.87f, 0.27f, 0.36f, 0.85f) : new Color(0.65f, 0.71f, 0.82f, 0.85f);
                _linha.startColor = c;
                _linha.endColor = new Color(c.r, c.g, c.b, 0.15f);
            }

            _linha.enabled = true;
            int n = Mathf.Max(0, Rota.Count - _indice);
            _linha.positionCount = n + 1;
            _linha.SetPosition(0, transform.position);
            for (int i = 0; i < n; i++) _linha.SetPosition(i + 1, Rota[_indice + i]);
        }

        protected virtual void OnDestroy()
        {
            if (_linha != null && _linha.material != null) Destroy(_linha.material);
        }
    }
}
