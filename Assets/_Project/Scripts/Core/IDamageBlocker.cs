namespace Odisea.Core
{
    /// <summary>
    /// Algo que puede detener un golpe antes de que quite vida (el escudo de hoja de plátano de Ulises).
    /// <see cref="Health"/> lo busca en su mismo objeto.
    /// </summary>
    public interface IDamageBlocker
    {
        /// <returns>true si detuvo el golpe (no se pierde vida).</returns>
        bool TryBlock(DamageInfo info);
    }
}
