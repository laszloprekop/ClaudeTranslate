// The window-render tests own the Avalonia UI thread; nothing else in this assembly may touch
// Avalonia objects at the same time.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
