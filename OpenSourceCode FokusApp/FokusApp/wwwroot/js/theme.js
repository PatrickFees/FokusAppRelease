window.fokusTheme = {
    apply: function (themeName, modeName) {
        const root = document.documentElement;
        root.setAttribute('data-theme', themeName);
        root.setAttribute('data-mode', modeName);
    }
};
