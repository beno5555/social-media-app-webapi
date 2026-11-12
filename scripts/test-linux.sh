#!/usr/bin/env bash
# Runs the integration tests on a Linux machine with Docker (a Claude Code cloud session, CI) against a
# throwaway SQL Server container. It does not read or depend on a developer's local Windows setup:
# every secret is generated here, and an existing appsettings.Testing.json is left untouched.
#
# Usage: scripts/test-linux.sh [dotnet test arguments]
#   scripts/test-linux.sh --filter "FullyQualifiedName~PostControllerTests"
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
STATE_DIR="${HOME}/.webcity-test"
PASSWORD_FILE="${STATE_DIR}/sa-password"
SETTINGS_FILE="${REPO_ROOT}/backend/aspnetproject/appsettings.Testing.json"

CONTAINER="webcity-test-sql"
IMAGE="mcr.microsoft.com/mssql/server:2022-latest"
DATABASE="TestSocialMediaWebApiDb"
READY_TIMEOUT_SECONDS=90

fail() {
    echo "test-linux: $1" >&2
    exit 1
}

wait_until() {
    local description="$1"
    shift

    local waited=0
    until "$@" > /dev/null 2>&1; do
        if (( waited >= READY_TIMEOUT_SECONDS )); then
            fail "timed out waiting for ${description}"
        fi
        sleep 1
        waited=$(( waited + 1 ))
    done
}

sql_server_answers() {
    docker exec -e SQLCMDPASSWORD="${SA_PASSWORD}" "${CONTAINER}" \
        /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -Q "SELECT 1"
}

command -v dotnet > /dev/null || fail "the .NET SDK is missing (Ubuntu: apt-get install -y dotnet-sdk-10.0)"
command -v docker > /dev/null || fail "Docker is missing"

if ! docker info > /dev/null 2>&1; then
    mkdir -p "${STATE_DIR}"
    nohup dockerd > "${STATE_DIR}/dockerd.log" 2>&1 &
    wait_until "the Docker daemon" docker info
fi

# The password lives outside the repo. Without it an existing container cannot be used, so it is rebuilt.
if [[ ! -f "${PASSWORD_FILE}" ]]; then
    mkdir -p "${STATE_DIR}"
    docker rm -f "${CONTAINER}" > /dev/null 2>&1 || true
    ( umask 077 && echo "$(openssl rand -hex 16)Aa1" > "${PASSWORD_FILE}" )
fi
SA_PASSWORD="$(cat "${PASSWORD_FILE}")"

if ! docker container inspect "${CONTAINER}" > /dev/null 2>&1; then
    docker run -d --name "${CONTAINER}" \
        -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD="${SA_PASSWORD}" \
        -p 127.0.0.1:1433:1433 "${IMAGE}" > /dev/null
else
    docker start "${CONTAINER}" > /dev/null
fi
wait_until "SQL Server" sql_server_answers

# Non-secret settings only. The connection string and JWT key come from the environment below.
if [[ ! -f "${SETTINGS_FILE}" ]]; then
    cat > "${SETTINGS_FILE}" <<'JSON'
{
  "Jwt": { "AccessTokenMinutes": 15, "RefreshTokenDays": 7 },
  "PasswordConfiguration": { "ResetTokenMinutes": 15 },
  "EmailConfiguration": { "Enabled": false },
  "Cors": { "AllowedOrigins": [ "http://localhost:4200" ] }
}
JSON
fi

export ConnectionStrings__DefaultConnection="Server=localhost;Database=${DATABASE};User Id=sa;Password=${SA_PASSWORD};TrustServerCertificate=True"
export Jwt__Key="$(openssl rand -hex 48)"

cd "${REPO_ROOT}"
exec dotnet test backend/aspnetproject.IntegrationTests "$@"
