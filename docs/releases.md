# Tag-based releases

Jenkins builds and tests pull requests and branches. The `develop` deployment
stage remains a placeholder. Release branches no longer trigger the release stage.

Tags must match `vMAJOR.MINOR.PATCH`, with no leading zeros, suffixes or build
metadata. Jenkins rejects invalid tags, verifies that the tag resolves to the
checked-out commit, and requires that commit to be reachable from `origin/main`.
Annotated and lightweight tags are supported. The check fetches current `main`
and completes shallow history before testing ancestry.

## Jenkins prerequisites

- Use a Multibranch Pipeline with tag discovery enabled in its SCM source.
- Ensure the build strategy schedules discovered tags (for example, the
  **Build tags** strategy from Basic Branch Build Strategies). Keep branch and
  pull request builds enabled as well.
- Configure a webhook or periodic indexing so newly pushed tags are discovered.
- The Linux agent needs Git, the pinned .NET SDK, Docker with daemon access,
  PowerShell 7.4 or later (`pwsh`) and Azure CLI with the `containerapp` commands available.
- SCM credentials must allow fetching `main`, tags and complete repository history.
- Folder credential `azure-poineer-server-deploy` must be a Jenkins
  **Username with password** credential: client ID as username, client secret
  **value** as password. The Credentials Binding plugin provides `withCredentials`.
- The service principal needs `AcrPush` on `poineerdevacr` (RBAC Registry
  Permissions mode) and `Container Apps Contributor` on `poineer-api-dev` only.
- The existing app must use Single revision mode and retain its managed identity
  with `AcrPull` on the registry and catalog read access. Jenkins does not create
  identities, infrastructure or role assignments.
- Jenkins jobs must be trusted: untrusted pull requests must not be allowed to
  modify pipeline code and access folder credentials. Stage conditions are not
  a security boundary against malicious Jenkinsfile changes.

The Jenkinsfile can validate discovered tags but cannot configure the Jenkins
job's discovery or scheduling settings. Verify those settings in Jenkins.

## Release workflow

1. Merge the release changes into `main` through the normal review process.
2. Fetch `main` and choose its tested release commit.
3. Create and push an annotated tag:

   ```sh
   git fetch origin
   git tag -a v0.3.1 origin/main -m "Release 0.3.1"
   git push origin v0.3.1
   ```

4. Check Jenkins: tag validation, build, .NET tests and offline deployment tests
   must pass before the release container is built. The release stage runs only
   for tags and has a 20-minute timeout.

For `v0.3.1`, the .NET version is `0.3.1`, the container is
`poineerdevacr.azurecr.io/poineer-server:v0.3.1`, and `/api/health` reports
`0.3.1`. Source revision metadata
is omitted from the health version. Local and branch builds default to
`0.3.0-dev`; API routes remain at `/api/v1`.

Do not move or overwrite published release tags. Use a new version for fixes.

## Deployment behavior

The Jenkinsfile explicitly configures the tenant, subscription, registry,
repository, resource group and app. Secrets remain in Jenkins Credentials.
`scripts/Deploy-Release.ps1` and its `ReleaseDeployment.psm1` module authenticate
using temporary private Azure CLI and
Docker configuration directories and cleans them up on success, failure and
normal termination. A forced process kill or host crash can leave directories
under the agent's temporary directory; remove stale `poineer-auth-*` directories
only after verifying no release job is using them.

The script rejects existing release tags, pushes the locally built image, locks
the tag and manifest against writes and deletion, and resolves the image digest.
It updates only the `api` container image in `poineer-api-dev`, preserving existing
configuration and identity. It waits for the new ready revision and checks the
public health endpoint for `Healthy` and the exact release version.

A file lock serializes this script's releases on the current agent. Keep
this as the only publisher of these release tags: the initial existence check
and image push are not an atomic registry operation. Additional agents or manual
publishers require shared serialization. Do not bypass the registry locks.

Only the existing development Container App is updated; this does not create a
production environment. The `develop` deployment stage remains a placeholder.
Version 0.3.0 used a release placeholder; automatic deployment starts with a tag
whose commit contains this implementation.

## Local PowerShell verification

Run the offline tests on Windows or Linux without Azure credentials, Docker or
additional PowerShell modules:

```powershell
pwsh -NoProfile -File scripts/Test-ReleaseDeployment.ps1
```

The tests simulate Azure and Docker operations and exercise native process exit
codes and health-check retries. They do not deploy anything. Jenkins runs the
same tests for branches, pull requests and tags.

Check that the Jenkins service account can run PowerShell before pushing:

```sh
sudo -u jenkins -H pwsh -NoProfile -Command '$PSVersionTable.PSVersion'
```

The deployment entry point is `scripts/Deploy-Release.ps1`. Run it only with the
documented release environment variables and credentials. Its module also
supports Windows with the standard Azure CLI installation, invoking the bundled
CLI Python runtime directly instead of passing secret arguments through `cmd.exe`.
No separate Python installation is needed by these deployment scripts.

The existing manual [Azure development deployment](azure-dev.md) remains
available. For a manual versioned image build, pass the version explicitly:

```sh
docker build --build-arg VERSION=0.3.1 --tag poineer-server:v0.3.1 .
```

## Credential rotation

Record the client secret expiry in the password manager and set reminders before
expiry. Create a replacement secret, update the password of the existing Jenkins
credential, verify authentication, then remove the old secret. Keep the credential
ID and client ID unchanged. Never commit or print secret values.

## Troubleshooting and rollback

- Authentication failures: check tenant, client ID, secret value and expiry.
- Authorization failures: verify the resource-scoped roles and allow time for
  role assignment propagation. Do not broaden access to the subscription.
- CLI errors: verify `az containerapp update --help` works for the Jenkins user;
  provision required CLI components on the agent before running releases.
- An existing image tag fails deliberately. If deployment failed after publishing,
  retain the image and use its digest for an operator-controlled recovery, or
  create a new patch release. Rerunning publication will not overwrite it.
- Readiness or health failures: inspect Container App revisions, pull permissions,
  logs and traffic. The script does not automatically roll back.

The pipeline prints the previous image before updating. To roll back, an operator
can update the app to that recorded image, preferably a digest, then verify health
and the previous version:

```sh
az containerapp update --resource-group rg-poineer-dev --name poineer-api-dev \
  --container-name api --image '<previous-image-reference>'
```

Never delete the shared resource group or renderer resources as rollback steps.

## Pipeline verification

Before the first release, verify these cases in the Jenkins multibranch job:

| Input | Expected result |
| --- | --- |
| Branch or pull request | Build and tests only; no release container/stage |
| `develop` | Build, tests and existing development placeholder |
| `v0.3.1` on `main` | Tests, publish locked image, deploy digest, verify health version `0.3.1` |
| `v01.3.0`, `v0.3.0-rc.1` or another invalid tag | Fail before build/release |
| Valid tag on a commit outside `main` | Fail before build/release |
| Build, test or container build failure | No release stage |
| Existing ACR tag | Fail without overwriting the image or updating the app |
| Publish failure | No Container App update; temporary credentials removed |
| Readiness or health failure | Release fails; previous image available in logs for rollback |

Use a disposable job/repository for negative tag cases; do not publish test tags
as project releases.
