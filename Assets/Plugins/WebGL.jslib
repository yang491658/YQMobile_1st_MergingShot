mergeInto(LibraryManager.library, {
  GameOverReact: function() {
    if (typeof window !== 'undefined' && typeof window.GameOverReact === 'function') {
      window.GameOverReact();
    } else if (typeof window !== 'undefined' && window.parent && typeof window.parent.GameOverReact === 'function') {
      window.parent.GameOverReact();
    }
  },
  ReplayReact: function() {
    if (typeof window !== 'undefined' && typeof window.ReplayReact === 'function') {
      window.ReplayReact();
    } else if (typeof window !== 'undefined' && window.parent && typeof window.parent.ReplayReact === 'function') {
      window.parent.ReplayReact();
    }
  }
});
