// -----------------------------------------------------------------------
// <copyright file="footer.js" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

import { fetchAppInfo } from './api.js';

export async function renderAppVersion() {
    const targets = document.querySelectorAll('[data-app-version-label]');
    if (targets.length === 0) return;

    try {
        const info = await fetchAppInfo();
        const version = info.version || info.assemblyVersion;
        if (!version) return;

        targets.forEach(target => {
            target.textContent = `SkillServer v${version}`;
        });
    } catch {
        // Leave the static fallback intact when the runtime endpoint is unavailable.
    }
}
