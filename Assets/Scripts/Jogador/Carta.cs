using UnityEngine;
using CartaBranca.Nucleo;

namespace CartaBranca.Jogador
{
    /// <summary>Projetil da Carta Branca. Nao conhece Inimigo: conversa apenas
    /// com a interface IDanificavel (polimorfismo por interface).</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Carta : MonoBehaviour
    {
        [SerializeField] float velocidade = 17f;
        [SerializeField] float duracao = 1.6f;
        [SerializeField] int dano = 1;
        [SerializeField] float giro = 720f;
        [SerializeField] GameObject efeitoImpacto;

        Vector2 _direcao = Vector2.right;
        float _morreEm;

        public void Lancar(Vector2 direcao, GameObject impacto)
        {
            _direcao = direcao.normalized;
            efeitoImpacto = impacto;
            _morreEm = Time.time + duracao;
        }

        void OnEnable()
        {
            _morreEm = Time.time + duracao;
        }

        void Update()
        {
            transform.position += (Vector3)(_direcao * velocidade * Time.deltaTime);
            transform.Rotate(0f, 0f, giro * Time.deltaTime * (_direcao.x >= 0f ? -1f : 1f));
            if (Time.time >= _morreEm) Destroy(gameObject);
        }

        void OnTriggerEnter2D(Collider2D outro)
        {
            if (outro.CompareTag("Player")) return;

            IDanificavel alvo = outro.GetComponentInParent<IDanificavel>();
            if (alvo != null && alvo.Vivo)
            {
                alvo.Danificar(dano, transform.position);
                Estourar();
                return;
            }

            // bateu no cenario (colisor solido)
            if (!outro.isTrigger) Estourar();
        }

        void Estourar()
        {
            if (efeitoImpacto != null)
                Instantiate(efeitoImpacto, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
    }
}
