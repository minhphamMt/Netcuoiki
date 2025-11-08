// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.
// Custom scripts for the BTAPLON LMS interface
(function () {
    const body = document.body;
    const toggles = document.querySelectorAll('[data-sidebar-toggle]');
    const overlay = document.querySelector('.lms-sidebar-overlay');

    // Write your JavaScript code.
    const closeSidebar = () => {
        body.classList.remove('sidebar-open');
    };

    const toggleSidebar = () => {
        body.classList.toggle('sidebar-open');
    };

    toggles.forEach(toggle => {
        toggle.addEventListener('click', toggleSidebar);
    });

    if (overlay) {
        overlay.addEventListener('click', closeSidebar);
    }

    window.addEventListener('resize', () => {
        if (window.innerWidth >= 992) {
            closeSidebar();
        }
    });
})();