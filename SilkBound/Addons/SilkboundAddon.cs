namespace SilkBound.Addons
{
    public abstract class SilkboundAddon
    {
        protected AddonLogger Logger => new(Name);
        public abstract string Name { get; }
        public abstract void OnEnable();
        public virtual void OnDisable(){}
    }
}