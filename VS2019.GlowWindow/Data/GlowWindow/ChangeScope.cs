namespace VS2019.GlowWindow.Data
{
    internal class ChangeScope : DisposableObject
    {
        private readonly Controls.GlowWindow _window;

        public ChangeScope(Controls.GlowWindow window)
        {
            _window = window;
            _window.DeferGlowChangesCount++;
        }

        protected override void DisposeManagedResources()
        {
            _window.DeferGlowChangesCount--;
            if (_window.DeferGlowChangesCount == 0) _window.EndDeferGlowChanges();
        }
    }
}
