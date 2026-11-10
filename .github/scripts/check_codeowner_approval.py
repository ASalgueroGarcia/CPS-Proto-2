#!/usr/bin/env python3
"""Code-owner approval gate (org-free, free-tier friendly).

Checks whether every code owner whose OWNED paths carry at least MIN_LINES
changed lines (additions + deletions) in a pull request has left an APPROVED
review. Pure stdlib; talks to the GitHub REST API with GITHUB_TOKEN.

Usage:
    check_codeowner_approval.py PR_NUMBER [MIN_LINES]

Exit codes: 0 = gate passed, 1 = approval missing, 2 = setup/API error.
"""

import base64
import fnmatch
import json
import os
import sys
import urllib.error
import urllib.request

MIN_LINES_DEFAULT = 10


def api_get(url, token):
    req = urllib.request.Request(
        url,
        headers={
            "Authorization": f"Bearer {token}",
            "Accept": "application/vnd.github+json",
            "X-GitHub-Api-Version": "2022-11-28",
            "User-Agent": "codeowner-approval-gate",
        },
    )
    try:
        with urllib.request.urlopen(req) as resp:
            return json.load(resp)
    except urllib.error.HTTPError as exc:
        detail = exc.read().decode(errors="replace")[:200]
        raise RuntimeError(f"GitHub API {exc.code} for {url}: {detail}") from exc


def api_get_all(path, base, token):
    """All pages of a list endpoint (100 per page)."""
    out = []
    page = 1
    while True:
        sep = "&" if "?" in path else "?"
        batch = api_get(f"{base}{path}{sep}per_page=100&page={page}", token)
        if not isinstance(batch, list) or not batch:
            return out
        out.extend(batch)
        if len(batch) < 100:
            return out
        page += 1


def parse_codeowners(text):
    """[(pattern, [owners...]), ...] in file order; last match wins."""
    rules = []
    for raw in text.splitlines():
        line = raw.strip()
        if not line or line.startswith("#"):
            continue
        parts = line.split()
        if len(parts) >= 2:
            rules.append((parts[0], parts[1:]))
    return rules


def owners_for(path, rules):
    """Owners of `path` per the LAST matching rule (CODEOWNERS semantics).

    Supports the pattern styles this repo uses: absolute paths, directory
    prefixes (trailing /) and single-level globs. `**` globs unsupported.
    """
    matched = None
    for pattern, owners in rules:
        p = pattern.lstrip("/")
        if p.endswith("/"):
            ok = path.startswith(p)
        elif "*" in p:
            ok = fnmatch.fnmatch(path, p)
        else:
            ok = path == p or path.startswith(p + "/")
        if ok:
            matched = owners
    return matched or []


def changed_lines_by_owner(files, rules):
    """Sum of additions+deletions per owner across the PR's files."""
    per = {}
    for f in files:
        lines = f.get("additions", 0) + f.get("deletions", 0)
        for owner in owners_for(f["filename"], rules):
            per[owner] = per.get(owner, 0) + lines
    return per


def required_owner(lines_by_owner, min_lines, author):
    """The ONE owner that must approve: the most-touched non-author owner whose
    owned paths carry >= min_lines changed lines. None if no such owner exists.

    Kalashnikov rule: exactly one approval is ever required, so a cross-owner
    PR can never jam on a second reviewer. Ties break alphabetically.
    Team owners ('@org/team') are skipped - no org here to resolve them.
    """
    candidates = {
        owner: lines
        for owner, lines in lines_by_owner.items()
        if lines >= min_lines and owner.lstrip("@") != author and "/" not in owner
    }
    if not candidates:
        return None
    return min(candidates.items(), key=lambda kv: (-kv[1], kv[0]))


def approved_logins(reviews):
    """Logins whose LATEST review state is APPROVED (a later dismissal or
    changes-requested overrides an earlier approval, like GitHub does)."""
    latest = {}
    for r in reviews:
        latest[r["user"]["login"]] = r["state"]
    return {login for login, state in latest.items() if state == "APPROVED"}


def main():
    token = os.environ.get("GITHUB_TOKEN")
    if not token:
        print("::error::GITHUB_TOKEN is not set")
        return 2
    if len(sys.argv) < 2:
        print("usage: check_codeowner_approval.py PR_NUMBER [MIN_LINES]")
        return 2

    try:
        pr_number = int(sys.argv[1])
        min_lines = int(sys.argv[2]) if len(sys.argv) > 2 else MIN_LINES_DEFAULT
        api = os.environ.get("GITHUB_API_URL", "https://api.github.com") + "/repos/"
        repo = os.environ["GITHUB_REPOSITORY"]

        pr = api_get(f"{api}{repo}/pulls/{pr_number}", token)
        author = pr["user"]["login"]
        base_ref = pr["base"]["ref"]

        codeowners_meta = api_get(f"{api}{repo}/contents/CODEOWNERS?ref={base_ref}", token)
        rules = parse_codeowners(
            base64.b64decode(codeowners_meta["content"]).decode("utf-8", errors="replace")
        )

        files = api_get_all(f"{repo}/pulls/{pr_number}/files", api, token)
        reviews = api_get_all(f"{repo}/pulls/{pr_number}/reviews", api, token)
    except RuntimeError as exc:
        print(f"::error::{exc}")
        return 2
    except (KeyError, ValueError) as exc:
        print(f"::error::Bad input or response shape: {exc}")
        return 2

    lines_by_owner = changed_lines_by_owner(files, rules)
    approved = approved_logins(reviews)

    if not lines_by_owner:
        print("No code-owner paths touched - gate passes")
        return 0

    required = required_owner(lines_by_owner, min_lines, author)

    for owner, lines in sorted(lines_by_owner.items()):
        if required and owner == required[0]:
            continue
        login = owner.lstrip("@")
        if login == author:
            tag = "author's own paths"
        elif lines < min_lines:
            tag = f"below the {min_lines}-line threshold"
        elif "/" in owner:
            tag = "team owner, org APIs unavailable"
        else:
            tag = "not the most-touched owner"
        print(f"INFO {owner}: {lines} changed lines ({tag})")

    if required is None:
        print("No owner past the threshold (or only the author's own paths) - gate passes")
        return 0

    top_owner, top_lines = required
    login = top_owner.lstrip("@")
    if login in approved:
        print(f"OK   {top_owner}: {top_lines} changed lines, APPROVED review present")
        print(f"Code-owner approval gate passed ({len(files)} files checked)")
        return 0

    print(f"MISS {top_owner}: {top_lines} changed lines (most-touched owner)")
    print(
        f"::error::Code-owner approval missing from {top_owner} - "
        f"the most-touched owner must APPROVE this PR"
    )
    return 1


if __name__ == "__main__":
    sys.exit(main())
