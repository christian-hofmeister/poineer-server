pipeline {
    agent any

    environment {
        DOTNET_VERSION = "9.0.303"
        SOLUTION_FILE  = "POIneer.Server.sln"
        TEST_PROJECT   = "tests/POIneer.Server.Tests/POIneer.Server.Tests.csproj"
    }

    options {
        skipDefaultCheckout true
    }

    stages {
        stage('Clean Workspace') {
            steps {
                cleanWs()
            }
        }

        stage('Checkout') {
            steps {
                checkout scm
            }
        }

        stage('Restore') {
            steps {
                script {
                    echo "Restoring .NET dependencies..."
                    sh "dotnet restore ${SOLUTION_FILE}"
                }
            }
        }

        stage('Install Tools') {
            steps {
                script {
                    echo "Restoring .NET local tools..."
                    // Tool-Manifest anlegen, falls nicht vorhanden
                    sh "[ -f .config/dotnet-tools.json ] || dotnet new tool-manifest"
                    // Tool installieren, falls nicht vorhanden
                    sh "dotnet tool install dotnet-outdated-tool || true"
                    // Tools synchronisieren
                    sh "dotnet tool restore"
                }
            }
        }

        stage('Check for outdated packages') {
            steps {
                script {
                    echo "Checking for outdated NuGet packages..."
                    sh "dotnet tool run dotnet-outdated --fail-on-updates --ignore-failed-sources"
                }
            }
        }

        stage('Build') {
            steps {
                script {
                    echo "Building solution..."
                    sh "dotnet build ${SOLUTION_FILE} --configuration Release --no-restore"
                }
            }
        }

        stage('Test') {
            steps {
                script {
                    echo "Running unit tests..."
                    sh "dotnet test ${TEST_PROJECT} --configuration Release --no-build"
                }
            }
        }

        stage('Deploy') {
            when {
                anyOf {
                    branch 'release/*'
                    branch 'main'
                }
            }
            steps {
                script {
                    if (env.BRANCH_NAME.startsWith('release/')) {
                        echo "Release branch detected - future: deploy to staging environment."
                        sh "echo 'TODO: Implement staging deployment here.'"
                    }
                    if (env.BRANCH_NAME == 'main') {
                        echo "Main branch detected - future: deploy to production environment."
                        sh "echo 'TODO: Implement production deployment here.'"
                    }
                }
            }
        }
    }

    post {
        success {
            echo "Build and tests completed successfully."
        }
        failure {
            echo "Build failed. Please check the logs."
        }
    }
}
