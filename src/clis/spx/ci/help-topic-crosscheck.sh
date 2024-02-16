#!/usr/bin/env bash
set -euo pipefail

# This small script finds all mentions of "spx help" inside of help documents in this folder, recording referenced
# topics that don't exist or have only placeholder entries.


SCRIPT_DIR="$(dirname ${BASH_SOURCE[0]})"
HELP_DIR="$(realpath $SCRIPT_DIR/../.x/help)"

declare -A notFoundTopicToSourceMap
declare -A notWrittenTopicToSourceMap

printf "Scanning help files in $HELP_DIR (this can take a bit)..."
cd "$HELP_DIR"

for file in $(ls); do
  # Ignore script files (like this one)
  if [[ $file =~ .sh$ ]]; then continue; fi
  # Search for anything that matches "spx help ...", trimming space and ignoring parameterized (e.g. find) use
  readarray -t references < \
    <(cat $file \
        | tr -d '\r' \
        | sed -nr "s/.*spx help ([^\)]*).*/\1/p" \
        | sed -r "s/ *$//" \
        | sed -r "s/ /./g")
  for reference in ${references[@]}; do
    if [[ $reference =~ [-\*] ]]; then continue; fi
    if [[ ! $(ls $reference 2>/dev/null) ]]; then
        notFoundTopicToSourceMap[$reference]="${notFoundTopicToSourceMap[$reference]:-}$file,"
    elif [[ $(grep WRITTEN $reference) ]]; then
        notWrittenTopicToSourceMap[$reference]="${notWrittenTopicToSourceMap[$reference]:-}$file,"
    fi
  done
done

echo "done."
echo ""

echo "====="
echo "These ${#notFoundTopicToSourceMap[@]} articles don't exist at all:"
echo "====="
echo ""

echo ${!notFoundTopicToSourceMap[@]} | tr ' ' '\012' | sort
echo ""

# To do: could print the files referring to the non-existant articles here

echo "====="
echo "These ${#notWrittenTopicToSourceMap[@]} articles exist but contain 'WRITTEN' (not yet written):"
echo "====="
echo ""

echo ${!notWrittenTopicToSourceMap[@]} | tr ' ' '\012' | sort
echo ""

# To do: could print the files referring to the placeholder articles here