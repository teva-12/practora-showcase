#!/bin/bash
# ================================================================
#  ShopElegance — Test Runner Script
#  Usage: ./run-tests.sh [unit|integration|e2e|all|coverage]
# ================================================================

set -e

ROOT="$(cd "$(dirname "$0")" && pwd)"
TEST_PROJECT="$ROOT/src/ShopElegance.Tests"
COVERAGE_DIR="$ROOT/coverage"

RED='\033[0;31m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'; NC='\033[0m'

info()    { echo -e "${GREEN}[INFO]${NC} $*"; }
warn()    { echo -e "${YELLOW}[WARN]${NC} $*"; }
error()   { echo -e "${RED}[ERROR]${NC} $*"; exit 1; }

CMD="${1:-all}"

run_unit() {
    info "Running Unit Tests..."
    dotnet test "$TEST_PROJECT" \
        --filter "FullyQualifiedName~Unit" \
        --logger "console;verbosity=normal" \
        --no-build
}

run_integration() {
    info "Running Integration Tests..."
    dotnet test "$TEST_PROJECT" \
        --filter "FullyQualifiedName~Integration" \
        --logger "console;verbosity=normal" \
        --no-build
}

run_e2e() {
    info "Running E2E Tests..."
    dotnet test "$TEST_PROJECT" \
        --filter "FullyQualifiedName~E2E" \
        --logger "console;verbosity=normal" \
        --no-build
}

run_all() {
    info "Running ALL Tests..."
    dotnet test "$TEST_PROJECT" \
        --logger "console;verbosity=normal" \
        --logger "trx;LogFileName=TestResults.trx" \
        --no-build
}

run_coverage() {
    info "Running Tests with Coverage (target: 80%+)..."
    mkdir -p "$COVERAGE_DIR"

    dotnet test "$TEST_PROJECT" \
        --settings "$ROOT/coverlet.runsettings" \
        --results-directory "$COVERAGE_DIR" \
        --logger "console;verbosity=minimal" \
        --collect "XPlat Code Coverage" \
        --no-build

    # Generate HTML report if reportgenerator is available
    if command -v reportgenerator &> /dev/null; then
        info "Generating HTML coverage report..."
        reportgenerator \
            -reports:"$COVERAGE_DIR/**/coverage.cobertura.xml" \
            -targetdir:"$COVERAGE_DIR/html" \
            -reporttypes:Html \
            -title:"ShopElegance Coverage Report"
        info "HTML report: $COVERAGE_DIR/html/index.html"
    else
        warn "reportgenerator not found. Install with: dotnet tool install -g dotnet-reportgenerator-globaltool"
    fi
}

# Build first
info "Building solution..."
dotnet build "$ROOT/ShopElegance.sln" --configuration Debug -v q

case "$CMD" in
    unit)        run_unit ;;
    integration) run_integration ;;
    e2e)         run_e2e ;;
    coverage)    run_coverage ;;
    all)         run_all ;;
    *)           error "Unknown command: $CMD. Use: unit|integration|e2e|all|coverage" ;;
esac

info "Done ✓"
