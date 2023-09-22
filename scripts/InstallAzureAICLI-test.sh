#!/bin/bash

# check to see if ai is in the path using which
set +o errexit
if ! which ai > /dev/null; then
    echo "ai command not found in path" 1>&2
    echo "current path is $PATH" 1>&2

    source ~/.bashrc
    source ~/.zshrc

    # check again
    if ! which ai > /dev/null; then
        echo "ai command still not found in path" 1>&2
        echo "current path is $PATH" 1>&2
        exit 1
    fi
fi

set -o errexit
ai run --command "ai" --expect "ai init;ai chat;ai service" 1>&2
ai run --command "ai config @defaults" --expect "found at 'ai.exe/.ai/'" 1>&2
ai run --command "ai config @_test --set TEST" 1>&2
ai run --command "ai config @_test" --expect "TEST" 1>&2
ai run --command "ai config @_test --clear" 1>&2
ai run --command "ai config @_test" --not-expect "TEST" 1>&2
