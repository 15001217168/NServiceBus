namespace NServiceBus
{
    using JetBrains.Annotations;

    /// <summary>
    /// Tells the infrastructure that the user wants to handle a timeout of <typeparamref name="T"/>.
    /// </summary>
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public interface IHandleTimeouts<T>
    {
        /// <summary>
        /// Called when the timeout has expired.
        /// </summary>
        void Timeout(T state);
    }
}