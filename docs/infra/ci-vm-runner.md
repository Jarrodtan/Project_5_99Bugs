# Running CI on the team VM (GitHub self-hosted runner)

> **Status (22 Oct 2026): not pursued.** The VM has 3.7 GB RAM and Tomas confirmed it can't be upgraded, so Unity CI
> stays on GitHub-hosted runners (see [ci.md](ci.md)). The VM's CI role is now planned as running a **headless build**
> for soak tests (M3). This page is kept as the setup reference in case a bigger machine becomes available.

GitHub stays the same for the team (push, PRs, ✅/❌ checks). The **VM does the heavy work**: Unity tests and the Android build.
Why: GitHub's free CI minutes and artifact storage ran out in October 2026 and locked Actions. Self-hosted runners use no minutes, and APKs saved on the VM use no GitHub storage.

```
Push / PR on GitHub ──▶ job sent to the runner on team21.dp-ext8.com
                              │  Docker + Unity: tests, Android build
                              ▼
        ✅/❌ on the GitHub PR        APK saved in /srv/ecobalance/apks
                                      Milestone tag → GitHub Release
```

Teammates are not affected while this is being set up. Nothing changes until the workflow PR (Step 5) is merged.

---

## Checklist

| # | Step | Who | Time | Done |
|---|------|-----|------|------|
| 1 | Check the VM specs | Wei Leong | 2 min | ☐ |
| 2 | Unlock the GitHub account (pay ~$0.57, back to Pro, $0 budget) | Wei Leong | 10 min | ☐ |
| 3 | Install Docker (skip if already installed) | Wei Leong | 10 min | ☐ |
| 4 | Create the runner user and register the runner | Wei Leong | 20 min | ☐ |
| 5 | Change `unity-ci.yml` to use the VM (PR) | Claude | — | ☐ |
| 6 | Weekly Docker clean-up | Wei Leong | 2 min | ☐ |

---

## Step 1: Check the VM specs

```bash
ssh team21@team21.dp-ext8.com
```
```bash
nproc; free -h; df -h /; docker --version; cat /etc/os-release | head -2
```

| What | Minimum | Comfortable |
|------|---------|-------------|
| CPU cores (`nproc`) | 2 | 4+ |
| RAM (`free -h` → Mem total) | 8 GB | 16 GB |
| Free disk (`df -h /` → Avail) | 30 GB | 50 GB+ |
| OS | Ubuntu 20.04+ | Ubuntu 22.04 / 24.04 |

Below the minimum → **stop**. Unity builds will crash (exit 137, out of memory).

> **Result (21 Oct 2026):** team21 VM = 2 CPU, **3.7 GB RAM** (+2 GB swap), 33 GB free disk, Ubuntu 24.04, no Docker.
> **Too small for Unity CI.** Steps 3–6 are on hold until the VM is upgraded (asked Tomas for ~4 CPU / 16 GB RAM / 80 GB disk).
> Until then CI stays on GitHub-hosted runners with the lighter workflow (PRs: tests only; APK on `main` / tags / Run workflow).

---

## Step 2: Unlock the GitHub account

GitHub → avatar → **Settings → Billing and licensing**

1. **Payment information → Payment method → Add → PayPal** → approve in PayPal.
2. Pay the outstanding **~$0.57 USD** if asked.
3. **Licensing → Upgrade to GitHub Pro → Pay monthly** → check **Payment due: $0.00** (student coupon) → **Upgrade**.
4. **Budgets and alerts → Actions → budget $0** → tick **Stop usage when budget limit is reached**.
5. Comment on support ticket **#4881011**: paid, re-upgraded to Pro, please refund the $0.57 and confirm the coupon covers Pro.

✅ Done when **Overview → Subscriptions** shows **GitHub Pro**.

---

## Step 3: Install Docker (skip if Step 1 printed a Docker version)

```bash
curl -fsSL https://get.docker.com | sudo sh
```
```bash
sudo systemctl enable --now docker
```
```bash
sudo docker run --rm hello-world
```
✅ Done when it prints **"Hello from Docker!"**

---

## Step 4: Create the runner user and register the runner

**4a. Runner user and APK folder**
```bash
sudo useradd -m -s /bin/bash ghrunner
```
```bash
sudo usermod -aG docker ghrunner
```
```bash
sudo mkdir -p /srv/ecobalance/apks && sudo chown ghrunner:ghrunner /srv/ecobalance/apks
```

**4b. Get the registration commands**
GitHub → repo **Project_5_99Bugs → Settings → Actions → Runners → New self-hosted runner** → **Linux**, **x64**.
Keep the page open. It shows a **one-time token**. Don't share it.

**4c. Download and configure (as the runner user)**
```bash
sudo -iu ghrunner
```
Run the **Download** commands from the GitHub page (`mkdir actions-runner && cd actions-runner`, `curl …`, `tar xzf …`), then:
```bash
./config.sh --url https://github.com/Jarrodtan/Project_5_99Bugs --token <TOKEN-FROM-PAGE> --labels unity-vm
```
Press Enter for the defaults, then leave the runner user:
```bash
exit
```

**4d. Install as a service (starts after reboots)**
```bash
cd /home/ghrunner/actions-runner && sudo ./svc.sh install ghrunner && sudo ./svc.sh start
```

✅ Done when **Settings → Actions → Runners** shows the runner as **🟢 Idle**.

---

## Step 5: Workflow changes (Claude, as a PR)

| Change | Why |
|--------|-----|
| `runs-on: [self-hosted, linux, unity-vm]` | Send jobs to the VM |
| Remove the `free-disk-space` step | ⚠️ It deletes system files. Fine on GitHub's throwaway machines, harmful on the VM |
| LFS pulled from Gitea on the same machine | Faster |
| APK copied to `/srv/ecobalance/apks/<commit>/`, last 10 kept | No GitHub storage charges |
| Tag `v*` (e.g. `v0.2-m2`) → APK kept permanently on Gitea | Rubric "versioned releases"; no GitHub storage used |
| PRs: tests only. `main`, tags, Run workflow: tests + APK | Fewer runs |

Backup: if the VM is ever down, change `runs-on` back to `ubuntu-latest` (one line).

---

## Step 6: Weekly Docker clean-up

```bash
echo '0 4 * * 0 root docker image prune -af --filter "until=168h" && docker builder prune -af' | sudo tee /etc/cron.d/docker-cleanup
```

---

## Good to know

- **Unity licence:** uses the same repo secrets (`UNITY_SERIAL`, `UNITY_EMAIL`, `UNITY_PASSWORD`). Each job activates the licence and returns it at the end.
- **One job at a time:** a second PR waits in the queue.
- **First run is slow:** it downloads the Unity Docker image (~15 GB) once. Later runs are faster than GitHub's machines.
- **Security:** only works safely while the repo is **private**. Never make it public with this runner attached.
- **Getting an APK:** after a `main` build, it's in `/srv/ecobalance/apks/<commit>/` on the VM. For milestones, use the GitHub Release.
- **Approved by:** Tomas Arce (21 Oct 2026): *"This is why I gave you the VMs."*
