using UnityEngine;

namespace CartaBranca.Mundo
{
    /// <summary>Camera que segue a Carta Branca com folga e limites da arena.</summary>
    public class CameraSuave : MonoBehaviour
    {
        public Transform alvo;
        public float suavidade = 6f;
        public Vector2 deslocamento = new Vector2(0f, 1.2f);
        public float limiteEsquerda = -18f;
        public float limiteDireita = 18f;
        public float limiteBaixo = -2f;
        public float limiteCima = 8f;

        void LateUpdate()
        {
            if (alvo == null) return;
            Vector3 destino = new Vector3(
                Mathf.Clamp(alvo.position.x + deslocamento.x, limiteEsquerda, limiteDireita),
                Mathf.Clamp(alvo.position.y + deslocamento.y, limiteBaixo, limiteCima),
                transform.position.z);
            transform.position = Vector3.Lerp(transform.position, destino, 1f - Mathf.Exp(-suavidade * Time.deltaTime));
        }
    }
}
