# Publishing and Running Tests on GitHub

This document provides instructions on how to publish this Playwright .NET project to GitHub, configure the required secrets, and run workflows manually.

---

## 1. Initializing and Publishing to GitHub

We have configured a comprehensive `.gitignore` file to ensure your private `.env` configuration file containing real secrets is never tracked.

Open your terminal in the project root folder (`d:\Google Antigravity\Playwright-dotNet`) and run the following commands:

```bash
# 1. Initialize local git repository
git init -b main

# 2. Add files
git add .

# 3. Check git status to ensure .env is NOT tracked
git status

# 4. Commit files
git commit -m "Initial commit: Prepare project for publishing to GitHub with secret sanitization"

# 5. Add your remote GitHub repository (replace with your repo URL)
git remote add origin https://github.com/your-username/your-repo-name.git

# 6. Push to GitHub
git push -u origin main
```

---

## 2. Configuring Secrets on GitHub

To run the workflows successfully on GitHub, you need to configure GitHub Actions secrets. 

1. Go to your repository on GitHub.
2. Click on **Settings** (top bar).
3. In the left sidebar, navigate to **Secrets and variables** > **Actions**.
4. Click on the **New repository secret** button.
5. Add the following secrets:

| Secret Name | Value | Description |
| :--- | :--- | :--- |
| `SUPABASE_ANON_KEY` | `eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...` | The Supabase anonymous key from your `.env` file |
| `SUPABASE_SERVICE_ROLE_KEY` | `eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...` | The Supabase service role key from your `.env` file |

### Optional Secrets:
* `API_BASE_URL`: (Optional) If you want to override the default Supabase project API URL (`https://tjqznfdufdodbnjwapcv.supabase.co`).
* `BASE_URL`: (Optional) If you want to override the default E2E UI site URL (`https://even-hen.github.io/nest-family-app`).

---

## 3. Running Workflows Manually on GitHub

We have configured `workflow_dispatch` on both test suites so you can trigger them manually whenever you need to.

1. Go to your repository on GitHub.
2. Click on the **Actions** tab.
3. In the left sidebar, select either:
   - **API Tests** (for running backend tests)
   - **E2E UI Tests** (for running frontend UI tests)
4. Click the **Run workflow** dropdown button on the right side.
5. Select the branch (e.g., `main`) and click the green **Run workflow** button.
6. The test workflow will start running, and you will be able to view its logs and download report artifacts (like Allure Results or Screenshots/Traces if there's a failure).
