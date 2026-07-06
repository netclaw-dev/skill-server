// -----------------------------------------------------------------------
// <copyright file="router.js" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

export function getPathSegments() {
    const path = window.location.pathname.replace(/\/+$/, '');
    return path.split('/').filter(Boolean);
}

export function getSkillName() {
    const segments = getPathSegments();
    if (segments.length >= 2 && segments[0] === 'skills') {
        return decodeURIComponent(segments[1]);
    }
    return null;
}

export function getSubAgentName() {
    const segments = getPathSegments();
    if (segments.length >= 2 && segments[0] === 'subagents') {
        return decodeURIComponent(segments[1]);
    }
    return null;
}

export function isListingPage() {
    const segments = getPathSegments();
    return segments.length <= 1;
}
