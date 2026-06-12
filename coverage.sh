#!/bin/bash
set -e

export PATH="$PATH:/Users/davishe/.dotnet/tools"

TESTPROJECT="backend/SpeedClaim.Tests/SpeedClaim.Tests.csproj"
RESULTS_DIR="backend/SpeedClaim.Tests/TestResults"
REPORT_DIR="coverage-report"

echo "Running tests with coverage collection..."
dotnet test "$TESTPROJECT" \
  --collect:"XPlat Code Coverage" \
  --results-directory "$RESULTS_DIR" \
  -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.ExcludeByFile="**/Migrations/**"

echo "Generating HTML report..."
reportgenerator \
  -reports:"$RESULTS_DIR/**/coverage.cobertura.xml" \
  -targetdir:"$REPORT_DIR" \
  -reporttypes:"Html;TextSummary" \
  -assemblyfilters:"+SpeedClaim.Api" \
  -classfilters:"-*.Migrations.*"

echo ""
echo "Coverage summary:"
cat "$REPORT_DIR/Summary.txt"

echo ""
echo "Full HTML report: $REPORT_DIR/index.html"
