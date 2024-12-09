document.addEventListener('DOMContentLoaded', function () {
    // Get theme switcher button
    const themeSwitcher = document.getElementById('themeSwitcher');

    // Check for saved theme preference, otherwise use dark theme as default
    const savedTheme = localStorage.getItem('theme');
    const currentTheme = savedTheme || 'dark';

    // Set initial theme
    setTheme(currentTheme);

    // Theme switcher click handler
    themeSwitcher.addEventListener('click', function () {
        const html = document.documentElement;
        const currentTheme = html.getAttribute('data-bs-theme');
        const newTheme = currentTheme === 'light' ? 'dark' : 'light';

        setTheme(newTheme);
    });

    // Function to set theme
    function setTheme(theme) {
        const html = document.documentElement;
        const icon = themeSwitcher.querySelector('i');

        html.setAttribute('data-bs-theme', theme);

        // Update icon
        if (theme === 'dark') {
            icon.classList.remove('fa-moon');
            icon.classList.add('fa-sun');
        } else {
            icon.classList.remove('fa-sun');
            icon.classList.add('fa-moon');
        }

        // Save theme preference
        localStorage.setItem('theme', theme);
    }

    // Listen for system theme changes
    window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', function (e) {
        if (!localStorage.getItem('theme')) {
            setTheme(e.matches ? 'dark' : 'light');
        }
    });
});