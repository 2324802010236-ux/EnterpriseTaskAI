document.addEventListener("DOMContentLoaded", () => {
    const sidebar = document.getElementById("adminSidebar");
    const sidebarToggle = document.getElementById("sidebarToggle");
    const sidebarOverlay = document.getElementById("sidebarOverlay");
    const menuLinks = sidebar?.querySelectorAll(".sidebar-link") ?? [];

    const closeSidebar = () => document.body.classList.remove("sidebar-open");
    const normalizePath = path => path.replace(/\/+$/, "").toLowerCase() || "/";
    const currentPath = normalizePath(window.location.pathname);

    menuLinks.forEach(link => {
        link.classList.remove("active");

        const configuredPaths = link.dataset.activePaths
            ?.split(",")
            .map(normalizePath);
        const activePrefix = link.dataset.activePrefix
            ? normalizePath(link.dataset.activePrefix)
            : null;
        const linkPath = link.getAttribute("href") === "#"
            ? null
            : normalizePath(new URL(link.href, window.location.origin).pathname);

        if (configuredPaths?.includes(currentPath)
            || linkPath === currentPath
            || (activePrefix
                && (currentPath === activePrefix || currentPath.startsWith(`${activePrefix}/`)))) {
            link.classList.add("active");
        }

        link.addEventListener("click", closeSidebar);
    });

    sidebarToggle?.addEventListener("click", () => {
        document.body.classList.toggle("sidebar-open");
    });

    sidebarOverlay?.addEventListener("click", closeSidebar);
    document.addEventListener("keydown", event => {
        if (event.key === "Escape") {
            closeSidebar();
        }
    });
});
