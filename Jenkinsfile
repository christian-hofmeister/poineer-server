pipeline {
    agent any

    environment {
        DOTNET_SKIP_FIRST_TIME_EXPERIENCE = 'true'
        DOTNET_CLI_TELEMETRY_OPTOUT = 'true'
    }

    stages {
        stage('Checkout') {
            steps {
                checkout scm
            }
        }

        stage('Restore') {
            steps {
                sh 'dotnet restore'
            }
        }

        stage('Build') {
            steps {
                sh 'dotnet build --no-restore --configuration Release'
            }
        }

        stage('Test') {
            steps {
                sh 'dotnet test --no-build --configuration Release --verbosity normal'
            }
        }

        stage('Branch Logic') {
            steps {
                script {
                    if (env.BRANCH_NAME == 'main') {
                        echo '[INFO] Main-Branch erkannt → später: Deployment auf LIVE'
                    } else if (env.BRANCH_NAME.startsWith('release')) {
                        echo '[INFO] Release-Branch erkannt → später: Deployment auf DEV/Staging'
                    } else {
                        echo "[INFO] Kein Deployment vorgesehen für Branch: ${env.BRANCH_NAME}"
                    }
                }
            }
        }
    }

    post {
        failure {
            echo '❌ Build oder Tests fehlgeschlagen.'
        }
        success {
            echo '✅ Build & Tests erfolgreich abgeschlossen.'
        }
    }
}
