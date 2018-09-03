#!/usr/bin/env bash
set -e
set -o pipefail

curl -vL https://cache-redirector.jetbrains.com/maven-central/org/jetbrains/kotlin/kotlin-annotation-processing-gradle/1.2.41/kotlin-annotation-processing-gradle-1.2.41.pom

#pushd rider
#./gradlew --info --stacktrace buildPlugin
