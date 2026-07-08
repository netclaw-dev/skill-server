// -----------------------------------------------------------------------
// <copyright file="api.js" company="Petabridge, LLC">
//      Copyright (C) 2026 - 2026 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

const API_BASE = '/api/v1';

export async function fetchAppInfo() {
    const res = await fetch(`${API_BASE}/info`);
    if (!res.ok) throw new Error(`Failed to fetch app info: ${res.status}`);
    return res.json();
}

export async function fetchSkills(query, skip, take) {
    const params = new URLSearchParams();
    if (query) params.set('q', query);
    if (skip !== undefined) params.set('skip', skip.toString());
    if (take !== undefined) params.set('take', take.toString());
    const qs = params.toString();
    const url = `${API_BASE}/skills/${qs ? '?' + qs : ''}`;
    const res = await fetch(url);
    if (!res.ok) throw new Error(`Failed to fetch skills: ${res.status}`);
    return res.json();
}

export async function fetchSkillVersions(name) {
    const res = await fetch(`${API_BASE}/skills/${encodeURIComponent(name)}`);
    if (!res.ok) throw new Error(`Failed to fetch skill versions: ${res.status}`);
    return res.json();
}

export async function fetchSkillMd(name, version) {
    const res = await fetch(`${API_BASE}/skills/${encodeURIComponent(name)}/${encodeURIComponent(version)}/SKILL.md`);
    if (!res.ok) throw new Error(`Failed to fetch SKILL.md: ${res.status}`);
    return res.text();
}

export async function fetchSkillResources(name, version) {
    const res = await fetch(`${API_BASE}/skills/${encodeURIComponent(name)}/${encodeURIComponent(version)}/resources`);
    if (!res.ok) throw new Error(`Failed to fetch skill resources: ${res.status}`);
    return res.json();
}

export async function fetchSkillResource(name, version, path) {
    const res = await fetch(getSkillResourceUrl(name, version, path));
    if (!res.ok) throw new Error(`Failed to fetch skill resource: ${res.status}`);
    return res.text();
}

export function getSkillResourceUrl(name, version, path) {
    const encodedPath = path.split('/').map(encodeURIComponent).join('/');
    return `${API_BASE}/skills/${encodeURIComponent(name)}/${encodeURIComponent(version)}/${encodedPath}`;
}

export async function fetchSubAgents() {
    const res = await fetch(`${API_BASE}/subagents/`);
    if (!res.ok) throw new Error(`Failed to fetch subagents: ${res.status}`);
    return res.json();
}

export async function fetchSubAgentVersions(name) {
    const res = await fetch(`${API_BASE}/subagents/${encodeURIComponent(name)}`);
    if (!res.ok) throw new Error(`Failed to fetch subagent versions: ${res.status}`);
    return res.json();
}

export async function fetchAgentMd(name, version) {
    const res = await fetch(`${API_BASE}/subagents/${encodeURIComponent(name)}/${encodeURIComponent(version)}/agent.md`);
    if (!res.ok) throw new Error(`Failed to fetch agent.md: ${res.status}`);
    return res.text();
}
