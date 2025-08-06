pipeline {
    agent any

    environment {
        DOTNET_VERSION = "9.0.303"
        SOLUTION_FILE  = "POIneer.Server.sln"
        TEST_PROJECT   = "tests/POIneer.Server.Tests/POIneer.Server.Tests.csproj"
    }

    stages {
        stage('Clean') {
            steps {
                sh 'dotnet clean POIneer.Server.sln'
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
