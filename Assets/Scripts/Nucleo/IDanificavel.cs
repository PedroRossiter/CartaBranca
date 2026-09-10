using UnityEngine;

namespace CartaBranca.Nucleo
{
    /// <summary>Contrato de qualquer coisa que possa levar dano.
    /// Permite que a Carta (projetil) nao precise conhecer Inimigo nem Jogador.</summary>
    public interface IDanificavel
    {
        int Vida { get; }
        bool Vivo { get; }
        void Danificar(int dano, Vector2 origem);
    }
}
