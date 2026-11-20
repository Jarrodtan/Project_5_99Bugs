# Case study: responding to a compromised self-hosted Git server

**Team 99 Bugs** · **Role:** Tan Wei Leong (Tech Lead) · **When:** Sep–Oct 2026 · **Outcome:** reported, rebuilt securely, verified by the institution's IT team

## Context

GitHub was our main repository and ran CI. We also ran a small self-hosted **Gitea** server on a
team virtual machine, as a private copy of the repository and to try **Git LFS** for large Unity assets.

## What happened

While investigating a persistent **500 error** on the Gitea repository, I found the server had been
**compromised**: its Git configuration had been changed to run an unknown script on every Git operation.

- **Likely cause:** public sign-up was left open on an **outdated Gitea version** with a known weakness.
- **Impact:** the change also broke the configuration, so the script appears never to have run.
  GitHub, our source of truth, was not affected and no work was lost.

## How I responded

1. **Contained by not touching it.** No file changes, restarts or Git commands on the server, so evidence stayed intact.
2. **Collected evidence:** the changed configuration, the script, server logs and investigation notes.
3. **Reported it the same day** to the institution's IT helpdesk, with course staff in copy.
4. **Rebuilt from scratch** after IT reset the machine, following every point of their security advisory.
5. **Had the rebuild checked** by IT, who confirmed it was done properly.
6. **Kept delivery safe:** the team kept working on GitHub + CI throughout, and I postponed the
   Git LFS change until after the milestone so it could not put the build at risk.

## What the rebuild changed

| Principle | Before | After |
|---|---|---|
| Patch promptly | Outdated Gitea | Latest version |
| Secure defaults | Public sign-up open | Sign-up off, login required to view anything, admin protected with 2FA |
| Least exposure | — | Service bound to localhost behind an HTTPS reverse proxy; only the needed ports open |
| Least privilege | — | Service runs as its own non-root user |
| Verify, don't assume | — | Tested reachability from outside and checked the logs after each change |

Git LFS later moved to the rebuilt server over HTTPS, with CI pulling files using a read-only token
stored as a CI secret.

## What I learned

- **Preserve evidence, then report.** Don't quietly "fix" a compromised server; escalate with the facts.
- **Secure defaults and patching matter most.** The attack used open sign-up plus a known, already-fixed weakness.
- **Expose as little as possible** and run services with the least privilege they need.
- **Keep infrastructure separate from delivery,** so an incident on a side system never blocks the team.

---

*Drafted with AI assistance: Claude Code (Claude Opus 5.5) from the internal incident report. Reviewed and edited by Tan Wei Leong.*
