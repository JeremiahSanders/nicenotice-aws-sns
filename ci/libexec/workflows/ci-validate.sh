#!/usr/bin/env bash
# shellcheck disable=SC2155

###
# Validate the project's source, e.g. run tests, linting.
#
#   This script expects a CICEE CI library environment (which is provided when using 'cicee lib exec').
#   For CI library environment details, see: https://github.com/JeremiahSanders/cicee/blob/main/docs/use/ci-library.md
#
#   Workflow:
#   - Restore dependencies.
#   - Build all solution projects using 'Debug' configuration.
#   - Test all solution test projects.
###

set -o errexit  # Fail or exit immediately if there is an error.
set -o nounset  # Fail if an unset variable is used.
set -o pipefail # Fail pipelines if any command errors, not just the last one.

function ci-validate() {

  function __set_pwd() {
    printf "\n\nMoving from PWD ($(pwd)) to project root (${PROJECT_ROOT})\n" &&
      cd "${PROJECT_ROOT}"
  }
  
  function __display_environment() {
    printf "\n\n.NET environment:\n" &&
      dotnet --info &&
      printf "\n\n"
  }

  function __restore() {
    dotnet restore "${PROJECT_ROOT}"
  }

  function __build() {
    dotnet build "${PROJECT_ROOT}" \
      --configuration Debug \
      -p:Version="${PROJECT_VERSION_DIST}"
  }

  function __run_tests() {
    local reports_dir="${BUILD_ROOT}/reports"
    mkdir -p "${reports_dir}" &&
      dotnet test \
        --solution "${PROJECT_ROOT}" \
        --results-directory "${reports_dir}" \
        --report-trx \
        --output Detailed
  }

  printf "Beginning validation...\n\n" &&
    __set_pwd &&
    __display_environment &&
    __restore &&
    __build &&
    __run_tests &&
    printf "Validation complete!\n\n"
}

export -f ci-validate
