# Self-hosted Git server (Gitea): secure rebuild

**Team 21 (99 Bugs)** · **Owner:** Tan Wei Leong (Tech Lead) · **Status:** rebuilt and verified by the instructor (1 Oct 2026)

## Why we run Gitea

GitHub is our main repository and runs CI. We also host **Gitea** on the team21 VM as a private,
self-hosted copy of the repository and to try **Git LFS** for large Unity assets (models, audio).

## Timeline

| When | What happened |
|---|---|
| [TODO: date] | Gitea first installed on the team21 VM. |
| [TODO: date] | Notice received: [TODO: what was wrong, e.g. reachable from the internet, open sign-up, running as root]. |
| 30 Sep 2026 | Gitea reinstalled from scratch, following every point in the notice (see below). |
| 30 Sep 2026, 23:04 | Tried Git LFS on Gitea through an SSH tunnel (`8c03b3ae`). |
| 30 Sep 2026, 23:46 | LFS reverted until after M1 (`b58999f0`): pushes failed whenever the tunnel was down, and CI cannot reach `localhost`. GitHub stays the main pipeline. |
| 1 Oct 2026 | Instructor checked the rebuild and confirmed it was done properly. |

## What the rebuild does

| Requirement | How it is met |
|---|---|
| Private only | Gitea listens on `127.0.0.1:3000`; we reach it through an SSH tunnel. Tested as unreachable from outside. |
| No anonymous access | Sign-up is disabled, and a login is required even to view anything. |
| Minimal data | One account (ours) and only our own repository restored. |
| Nothing else exposed | No firewall holes, no Docker, no other services. |
| Least privilege | Gitea 1.27.1, running as the `gitea` user instead of root. |
| Verified | No requests from the internet since the rebuild. |

## What we learned

- **Least exposure:** bind services to localhost and reach them over SSH instead of opening ports.
- **Least privilege:** a service runs as its own user, never root.
- **Secure defaults:** turn off public sign-up, require a login, and expose only what is needed.
- **Verify, don't assume:** test from outside and check the logs after every change.
- **Keep infrastructure separate from delivery:** GitHub + CI stayed the main pipeline, and we reverted
  the LFS dependency before the deadline, so the Gitea work never put the M1 build at risk.

## Next (after M1)

If the team needs Gitea (e.g. for LFS or as a CI mirror), expose it on an approved public port with HTTPS
and keep sign-up off, so CI and teammates can reach it without an SSH tunnel.

---
*Drafted with AI assistance: Claude Code (Claude Opus 5.5, claude-opus-5-5) from the instructor's feedback and the git history. Reviewed and edited by Tan Wei Leong.*
