pipeline {
    agent any

    options {
        timestamps()
        disableConcurrentBuilds()
        buildDiscarder(logRotator(numToKeepStr: '20'))
    }

    environment {
        DOTNET_SDK_VERSION = '10.0.201'
        DOTNET_CLI_TELEMETRY_OPTOUT = '1'
        DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
        DOTNET_NOLOGO = '1'

        SOLUTION_FILE = 'POIneer.Server.slnx'
        CONFIGURATION = 'Release'

        AZURE_TENANT_ID = '441e2e97-9f7c-48cb-bad8-64680212588d'
        AZURE_SUBSCRIPTION_ID = 'c8e5e957-4471-4781-8530-d2f35c8f44e2'
        ACR_NAME = 'poineerdevacr'
        ACR_LOGIN_SERVER = 'poineerdevacr.azurecr.io'
        IMAGE_REPOSITORY = 'poineer-server'
        AZURE_RESOURCE_GROUP = 'rg-poineer-dev'
        AZURE_CONTAINER_APP = 'poineer-api-dev'

        NUGET_PACKAGES = "${WORKSPACE}/.nuget/packages"
    }

    stages {
        stage('Checkout') {
            steps {
                checkout scm
            }
        }

        stage('Resolve Release Version') {
            steps {
                script {
                    env.BUILD_VERSION = '0.3.0-dev'
                    if (env.TAG_NAME) {
                        if (!(env.TAG_NAME ==~ /v(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)/)) {
                            error('Release tags must match vMAJOR.MINOR.PATCH without leading zeros.')
                        }
                        sh '''
                            set -eu
                            if [ "$(git rev-parse --is-shallow-repository)" = true ]; then
                                git fetch --unshallow origin
                            fi
                            git fetch origin +refs/heads/main:refs/remotes/origin/main
                            git fetch origin "refs/tags/$TAG_NAME"
                            test "$(git rev-parse HEAD)" = "$(git rev-parse 'FETCH_HEAD^{commit}')"
                            git merge-base --is-ancestor HEAD refs/remotes/origin/main
                        '''
                        env.BUILD_VERSION = env.TAG_NAME.substring(1)
                        env.IMAGE_TAG = env.TAG_NAME
                    }
                }
            }
        }

        stage('Show .NET Info') {
            steps {
                sh 'dotnet --info'
            }
        }

        stage('Restore NuGet Packages') {
            steps {
                sh 'dotnet restore "$SOLUTION_FILE"'
            }
        }

        stage('Build Solution') {
            steps {
                sh '''
                    dotnet build "$SOLUTION_FILE" \
                        --configuration "$CONFIGURATION" \
                        --no-restore -p:Version="$BUILD_VERSION"
                '''
            }
        }

        stage('Test with Coverage') {
            steps {
                sh '''
                    set -eux

                    dotnet test "$SOLUTION_FILE" \
                        --configuration "$CONFIGURATION" \
                        --no-build \
                        --results-directory TestResults \
                        --logger "trx;LogFileName=test-results.trx" \
                        --collect:"XPlat Code Coverage;Format=cobertura"

                    echo "[diag] Coverage files:"
                    find . -type f -name "coverage.cobertura.xml" -print
                '''
            }

            post {
                always {
                    junit allowEmptyResults: true, testResults: 'TestResults/**/*.trx'

                    recordCoverage(
                        tools: [[
                            parser: 'COBERTURA',
                            pattern: '**/TestResults/**/coverage.cobertura.xml'
                        ]],
                        sourceCodeRetention: 'LAST_BUILD',
                        failOnError: true
                    )
                }
            }
        }

        stage('Test Release Orchestration') {
            steps {
                sh 'pwsh -NoProfile -File scripts/Test-ReleaseDeployment.ps1'
            }
        }

        stage('Dummy Deployment - Develop') {
            when {
                branch 'develop'
            }
            steps {
                echo 'Develop branch detected.'
                echo 'Dummy deployment stage for develop. No deployment is executed yet.'
            }
        }

        stage('Build Release Container') {
            when {
                buildingTag()
            }
            steps {
                sh 'docker build --build-arg VERSION="$BUILD_VERSION" --tag "poineer-server:$IMAGE_TAG" .'
            }
        }

        stage('Publish and Deploy Release') {
            when {
                buildingTag()
            }
            steps {
                timeout(time: 20, unit: 'MINUTES') {
                    withCredentials([usernamePassword(
                        credentialsId: 'azure-poineer-server-deploy',
                        usernameVariable: 'AZURE_CLIENT_ID',
                        passwordVariable: 'AZURE_CLIENT_SECRET'
                    )]) {
                        sh 'pwsh -NoProfile -File scripts/Deploy-Release.ps1'
                    }
                }
            }
        }

        stage('Feature Branch Info') {
            when {
                branch pattern: 'feature/.+', comparator: 'REGEXP'
            }
            steps {
                echo 'Feature branch detected. Build and tests only.'
            }
        }
    }

    post {
        success {
            echo 'Pipeline completed successfully.'
        }

        failure {
            echo 'Pipeline failed.'
        }

        always {
            cleanWs(
                deleteDirs: true,
                disableDeferredWipeout: true
            )
        }
    }
}
