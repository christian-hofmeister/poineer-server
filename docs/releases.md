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
- The Linux agent needs Git, the pinned .NET SDK and Docker with daemon access.
- SCM credentials must allow fetching `main`, tags and complete repository history.

The Jenkinsfile can validate discovered tags but cannot configure the Jenkins
job's discovery or scheduling settings. Verify those settings in Jenkins.

## Release workflow

1. Merge the release changes into `main` through the normal review process.
2. Fetch `main` and choose its tested release commit.
3. Create and push an annotated tag:

   ```sh
   git fetch origin
   git tag -a v0.3.0 origin/main -m "Release 0.3.0"
   git push origin v0.3.0
   ```

4. Check Jenkins: tag validation, build and tests must pass before the release
   container is built. The release stage runs only for tags.

For `v0.3.0`, the .NET version is `0.3.0`, the container is
`poineer-server:v0.3.0`, and `/api/health` reports `0.3.0`. Source revision metadata
is omitted from the health version. Local and branch builds default to
`0.3.0-dev`; API routes remain at `/api/v1`.

Do not move or overwrite published release tags. Use a new version for fixes.

## Deployment boundary

The repository currently has dummy Jenkins deployment stages and no configured
release environment, registry publication or release credentials. This pipeline
builds the versioned release image locally on the Jenkins agent; it does not push
or deploy it. Configure those operations in the release stage when a release
target exists. Do not treat the placeholder's success as a deployed release.

The existing manual [Azure development deployment](azure-dev.md) remains
available. For a manual versioned image build, pass the version explicitly:

```sh
docker build --build-arg VERSION=0.3.0 --tag poineer-server:v0.3.0 .
```

## Pipeline verification

Before the first release, verify these cases in the Jenkins multibranch job:

| Input | Expected result |
| --- | --- |
| Branch or pull request | Build and tests only; no release container/stage |
| `develop` | Build, tests and existing development placeholder |
| `v0.3.0` on `main` | Version `0.3.0`, tests, container `v0.3.0`, release placeholder |
| `v01.3.0`, `v0.3.0-rc.1` or another invalid tag | Fail before build/release |
| Valid tag on a commit outside `main` | Fail before build/release |
| Build, test or container build failure | No release stage |

Use a disposable job/repository for negative tag cases; do not publish test tags
as project releases.
