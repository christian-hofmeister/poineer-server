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

        NUGET_PACKAGES = "${WORKSPACE}/.nuget/packages"
    }

    stages {
        stage('Checkout') {
            steps {
                checkout scm
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
                        --no-restore
                '''
            }
        }

        stage('Run Tests') {
            steps {
                sh '''
                    dotnet test "$SOLUTION_FILE" \
                        --configuration "$CONFIGURATION" \
                        --no-build \
                        --logger "trx;LogFileName=test-results.trx" \
                        --results-directory TestResults
                '''
            }
            post {
                always {
                    junit allowEmptyResults: true, testResults: 'TestResults/**/*.trx'
                }
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

        stage('Dummy Deployment - Release') {
            when {
                branch pattern: 'release/.+', comparator: 'REGEXP'
            }
            steps {
                echo 'Release branch detected.'
                echo 'Dummy deployment stage for release. No deployment is executed yet.'
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