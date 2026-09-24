using UnityEngine;
using CartaBranca.Mundo;

namespace CartaBranca.Inimigos
{
    /// <summary>Naipe de Paus: fumaca com porrete que NAO atravessa plataformas.
    /// Calcula a rota A* ate a Carta Branca e contorna o cenario - nao ha esconderijo.
    /// Quando tem linha reta livre e esta perto, da o bote (acelera).</summary>
    public class InimigoPaus : InimigoNavegante
    {
        [Header("Paus")]
        [SerializeField] float alcanceDoBote = 3.2f;
        [SerializeField] float multiplicadorDoBote = 1.7f;
        [SerializeField] float balanco = 0.6f;

        float _fase;

        protected override void Awake()
        {
            base.Awake();
            _fase = Random.Range(0f, Mathf.PI * 2f);
        }

        /// <summary>Sobrescrita: Paus e mais duro que os outros (+1 de vida).</summary>
        public override void Configurar(float escala, int vida)
        {
            base.Configurar(escala, vida);
            DefinirVidaMaxima(vida + 1);
        }

        protected override Vector2 Destino()
        {
            if (Alvo != null && Alvo.Vivo) return (Vector2)Alvo.transform.position + Vector2.up * 0.2f;
            return transform.position;
        }

        protected override float VelocidadeAtual()
        {
            if (Alvo == null) return Velocidade;
            Vector2 pos = transform.position;
            Vector2 alvo = Alvo.transform.position;
            MapaDeNavegacao mapa = MapaDeNavegacao.Instancia;
            bool bote = Vector2.Distance(pos, alvo) < alcanceDoBote && (mapa == null || mapa.LinhaLivre(pos, alvo));
            return Velocidade * (bote ? multiplicadorDoBote : 1f);
        }

        protected override void Comportamento()
        {
            base.Comportamento();
            _fase += Time.deltaTime * 3f;
            transform.position += Vector3.up * Mathf.Sin(_fase) * balanco * Time.deltaTime;
        }
    }
}
