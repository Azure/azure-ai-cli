#!/bin/bash

echo "Source branch: $BUILD_SOURCEBRANCH"

# If the build was triggered from a tag, use the tag as the version. Otherwise, set the version to dev.
REGEX='^refs\/tags\/v?([[:digit:]]+)\.([[:digit:]]+)\.([[:digit:]]+)(-.+)?'
VERSION=$([[ $BUILD_SOURCEBRANCH =~ $REGEX ]] && echo $(echo $BUILD_SOURCEBRANCH | sed -r 's/'$REGEX'/\1.\2.\3\4/') || echo "0.1.0-dev")

# Set the AICLIVersion variable in the pipeline.
echo "##vso[task.setvariable variable=AICLIVersion]$VERSION"

# Set the AICLINuPkgFileName variable in the pipeline.
echo "##vso[task.setvariable variable=AICLINuPkgFileName]Azure.AI.CLI.$VERSION.nupkg"