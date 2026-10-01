# Self-hosted Git server (Gitea): secure rebuild

**Team 21 (99 Bugs)** · **Owner:** Tan Wei Leong (Tech Lead) · **Status:** incident reported 29 Sep, server rebuilt 30 Sep, verified by the instructor 1 Oct 2026

## Why we run Gitea

GitHub is our main repository and runs CI. We also host **Gitea** on the team21 VM as a private,
self-hosted copy of the repository and to try **Git LFS** for large Unity assets (models, audio).

## What happened

While investigating a persistent **500 Internal Server Error** on our repository, we found the Gitea
server had been **compromised**: the Git configuration of the Gitea service had been modified to run a
hidden script (`.pwn`) on every clone, fetch, pull and push. The script (~70 KB) downloads an external
binary and runs it in the background, disguised as a Gitea process.

- **Likely attack vector:** open user registration plus an outdated Gitea version (1.23.7). An attacker
  could create a throwaway account and abuse an internal logging endpoint to write into the config file.
- **Impact:** the malicious edit also corrupted the config, so Git operations failed (the 500 error) and the
  payload appears **not to have run**. Our source of truth (GitHub) was not affected.

## Timeline

| When | What happened |
|---|---|
| 29 Sep 2026 | Investigated the 500 error; found the malicious hook and the backdoor script (log analysis assisted by AI). |
| 29 Sep 2026, 16:28 | Reported the incident to DigiPen IT Helpdesk (instructor in copy) with the evidence: modified config, the script, 7 days of Gitea logs, attacker IPs found in the logs, and investigation notes. **Did not touch the server** (no file changes, no restart, no Git commands) to preserve evidence. |
| 30 Sep 2026, 15:59 | IT reset and rebuilt the server and sent a login guide and a security advisory. |
| 30 Sep 2026, night | Reinstalled Gitea from scratch, following every point in the advisory (below). |
| 30 Sep 2026, 23:04 | Tried Git LFS on Gitea through an SSH tunnel (`8c03b3ae`). |
| 30 Sep 2026, 23:46 | LFS reverted until after M1 (`b58999f0`): pushes failed whenever the tunnel was down, and CI cannot reach `localhost`. GitHub stays the main pipeline. |
| 1 Oct 2026 | Instructor checked the rebuild and confirmed it was done properly. |

## What the rebuild does

| Requirement          | How it is met                                                                                             |
| -------------------- | --------------------------------------------------------------------------------------------------------- |
| Private only         | Gitea listens on `127.0.0.1:3000`; we reach it through an SSH tunnel. Tested as unreachable from outside. |
| No anonymous access  | Sign-up is disabled, and a login is required even to view anything.                                       |
| Minimal data         | One account (ours) and only our own repository restored.                                                  |
| Nothing else exposed | No firewall holes, no Docker, no other services.                                                          |
| Up to date, least privilege | Gitea 1.27.1 (was 1.23.7), running as the `gitea` user instead of root.                      |
| Verified             | No requests from the internet since the rebuild.                                                          |

## What we learned

- **Least exposure:** bind services to localhost and reach them over SSH instead of opening ports.
- **Least privilege:** a service runs as its own user, never root.
- **Secure defaults:** turn off public sign-up, require a login, and expose only what is needed.
- **Patch promptly:** the attack used a known weakness in an outdated version.
- **Preserve evidence, then report:** don't "fix" a compromised server yourself; collect logs and files and escalate to IT.
- **Verify, don't assume:** test from outside and check the logs after every change.
- **Keep infrastructure separate from delivery:** GitHub + CI stayed the main pipeline, and we reverted
  the LFS dependency before the deadline, so the Gitea work never put the M1 build at risk.

## Next (after M1)

If the team needs Gitea (e.g. for LFS or as a CI mirror), expose it on an approved public port with HTTPS
and keep sign-up off, so CI and teammates can reach it without an SSH tunnel.

---

*Drafted with AI assistance: Claude Code (Claude Opus 5.5, claude-opus-5-5) from the incident report email, the instructor's feedback and the git history. Reviewed and edited by Tan Wei Leong.*
