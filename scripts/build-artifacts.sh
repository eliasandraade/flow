#!/usr/bin/env bash
#
# Builds the Sprint 2 delivery artifacts into dist/.
#
#   dist/backend/              published API + configuration, no secrets
#   dist/mobile/               mobile project, ready to build
#   dist/presentation-assets/  openapi.json, diagrams, endpoint list, test results
#
# Usage:  ./scripts/build-artifacts.sh
#
# Requires: .NET 8 SDK, Node 20+. A running MongoDB is only needed for the
# integration tests, which are skipped when FLOW_TEST_MONGO_URI is unset and no
# Docker daemon is available.

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DIST="$ROOT/dist"
STAGE="$DIST/staging"

cd "$ROOT"

echo "==> Cleaning dist/"
rm -rf "$DIST"
mkdir -p "$DIST/backend" "$DIST/mobile" "$DIST/presentation-assets" "$STAGE"

# ---------------------------------------------------------------------------
# Backend
# ---------------------------------------------------------------------------
echo "==> Building the backend"
dotnet publish src/Flow.API/Flow.API.csproj -c Release -o "$STAGE/api" --nologo

echo "==> Assembling dist/backend"
cp -r src "$DIST/backend/src"
cp -r tests "$DIST/backend/tests"
cp Flow.sln Dockerfile docker-compose.yml .env.example "$DIST/backend/"
cp README.md ENGINEERING.md PROJECT_DECISIONS.md "$DIST/backend/"
cp -r docs "$DIST/backend/docs"

# Build output and local settings never travel with source.
find "$DIST/backend" -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} + 2>/dev/null || true
find "$DIST/backend" -name "appsettings.*.local.json" -delete 2>/dev/null || true

# ---------------------------------------------------------------------------
# OpenAPI
# ---------------------------------------------------------------------------
echo "==> Exporting openapi.json"

# The specification is generated from the running application, so it reflects the
# real route table rather than a hand-maintained copy.
#
# That means a database is required, and not only for the health endpoint: startup seeds
# the Identity roles before serving anything, and aborts if it cannot reach MongoDB. An
# earlier version of this script claimed /swagger was served regardless. It is not, and
# the claim went unnoticed because the machine it was written on happened to have MongoDB
# running. Point Mongo__ConnectionString at whatever is available; a standalone server is
# enough here, since nothing on this path opens a transaction.
ASPNETCORE_ENVIRONMENT=Development \
ASPNETCORE_URLS=http://localhost:5199 \
Swagger__Enabled=true \
Mongo__EnsureIndexes=false \
Mongo__ConnectionString="${Mongo__ConnectionString:-mongodb://localhost:27017/?replicaSet=rs0}" \
Mongo__Database="${Mongo__Database:-flow_openapi_export}" \
JwtSettings__SecretKey="openapi-export-only-not-a-real-secret-32b" \
dotnet "$STAGE/api/Flow.API.dll" > "$STAGE/openapi-export.log" 2>&1 &
API_PID=$!

for _ in $(seq 1 40); do
  if curl -fsS "http://localhost:5199/swagger/v1/swagger.json" \
      -o "$DIST/presentation-assets/openapi.json" 2>/dev/null; then
    echo "    openapi.json exported"
    break
  fi
  sleep 1
done

kill "$API_PID" 2>/dev/null || true
wait "$API_PID" 2>/dev/null || true

# A warning here used to be enough, which meant the script could finish "successfully"
# having produced a dist/ with no specification in it. Failing is the honest outcome: the
# specification is a deliverable, not a nice-to-have.
if [ ! -s "$DIST/presentation-assets/openapi.json" ]; then
  echo "    ERROR: openapi.json could not be exported. The API did not start." >&2
  echo "    Last lines of its output:" >&2
  tail -n 20 "$STAGE/openapi-export.log" >&2 || true
  exit 1
fi

# ---------------------------------------------------------------------------
# Mobile
# ---------------------------------------------------------------------------
echo "==> Assembling dist/mobile"
mkdir -p "$DIST/mobile/flow-mobile"

# node_modules is reproducible from the lockfile and would dominate the archive.
tar --exclude=node_modules --exclude=.expo --exclude=dist \
    -cf - -C "$ROOT" mobile | tar -xf - -C "$DIST/mobile/flow-mobile" --strip-components=1

# ---------------------------------------------------------------------------
# Presentation assets
# ---------------------------------------------------------------------------
echo "==> Assembling dist/presentation-assets"
cp docs/sprint-2/*.md "$DIST/presentation-assets/"

echo "==> Recording the endpoint list"
if [ -s "$DIST/presentation-assets/openapi.json" ]; then
  node -e '
    const spec = require(process.argv[1]);
    const rows = [];
    for (const [path, methods] of Object.entries(spec.paths ?? {})) {
      for (const [method, op] of Object.entries(methods)) {
        rows.push([
          method.toUpperCase().padEnd(6),
          path.padEnd(52),
          (op.tags ?? []).join(",").padEnd(12),
          (op.summary ?? "").split("\n")[0],
        ].join(" "));
      }
    }
    rows.sort();
    console.log(`Flow API — ${rows.length} endpoints\n`);
    console.log(rows.join("\n"));
  ' "$DIST/presentation-assets/openapi.json" > "$DIST/presentation-assets/endpoints.txt"
fi

# FLOW_SKIP_TESTS exists for one situation: a pipeline where the suite already ran in an
# earlier job and the container image for MongoDB would have to be pulled a second time.
# It is opt-in, so running the script by hand always tests.
echo "==> Running the test suite"
if [ "${FLOW_SKIP_TESTS:-0}" = "1" ]; then
  echo "    skipped by FLOW_SKIP_TESTS; results recorded from the caller"
  echo "Test suite skipped in this run (FLOW_SKIP_TESTS=1)." > "$STAGE/test-output.txt"
elif dotnet test Flow.sln --nologo -v quiet > "$STAGE/test-output.txt" 2>&1; then
  echo "    tests passed"
else
  echo "    WARNING: some tests failed; see the recorded output" >&2
fi
grep -E "Aprovado!|Passed!|Com falha|Failed!" "$STAGE/test-output.txt" \
  > "$DIST/presentation-assets/test-results.txt" 2>/dev/null || \
  cp "$STAGE/test-output.txt" "$DIST/presentation-assets/test-results.txt"

# ---------------------------------------------------------------------------
# Archives
# ---------------------------------------------------------------------------
echo "==> Creating archives"
( cd "$DIST/backend" && tar -czf "$DIST/flow-backend.tar.gz" . )
( cd "$DIST/mobile" && tar -czf "$DIST/flow-mobile.tar.gz" . )

rm -rf "$STAGE"

echo
echo "Artifacts written to dist/:"
find "$DIST" -maxdepth 2 -type f -exec ls -lh {} \; | awk '{print "   ", $5, $9}'
echo
echo "The APK is built separately and needs EAS credentials:"
echo "    cd mobile && eas build --platform android --profile preview"
