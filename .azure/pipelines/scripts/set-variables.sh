#!/bin/bash

define_variable () {
    echo "$1=$2"
    echo "##vso[task.setvariable variable=$1;isOutput=true]$2"
}

echo "Source branch: $BUILD_SOURCEBRANCH"

# Determine the product version (major.minor.build).
# NOTE: If the major or minor version is not updated before a new year, the version number becomes ambiguous
# and it may not be possible to upgrade an old version from the previous year without manual uninstallation.
# Example:
# - last build of year N:    1.0.36599
# - first build of year N+1: 1.0.101   -> cannot update 1.0.36599 with this, must uninstall 1.0.36599 first.

MAJOR_VERSION="1"
MINOR_VERSION="0"
BUILD_VERSION="0"

if [ ! -z "$1" ]; then
    # e.g. "20240120.2" -> BuildYear 2024, BuildMonth 01, BuildDay 20, BuildRun 2
    BuildYear=$(echo "$1" | sed 's/^\([0-9]\{4\}\)[0-9]\{4\}\.[0-9]*$/\1/')
    BuildMonth=$(echo "$1" | sed 's/^[0-9]\{4\}\([0-9]\{2\}\)[0-9]\{2\}\.[0-9]*$/\1/')
    BuildDay=$(echo "$1" | sed 's/^[0-9]\{6\}\([0-9]\{2\}\)\.[0-9]*$/\1/')
    BuildRun=$(echo "$1" | sed 's/^[0-9]\{8\}\.\([0-9]*$\)/\1/')

    if [ ! -z "$BuildMonth" -a $BuildMonth -ge 1 -a $BuildMonth -le 12 -a \
         ! -z "$BuildDay"   -a $BuildDay   -ge 1 -a $BuildDay   -le 31 -a \
         ! -z "$BuildRun"   -a $BuildRun   -ge 1 -a $BuildRun   -le 99 ]
    then
        let DayOfYear="($BuildMonth - 1) * 31 + $BuildDay" # estimate using max days/month
        if [ $BuildRun -lt 10 ]; then
            BUILD_VERSION="${DayOfYear}0${BuildRun}"
        else
            BUILD_VERSION="${DayOfYear}${BuildRun}"
        fi
    else
        >&2 echo "Ignored invalid argument: BuildNumber $1"
    fi
fi
PRODUCT_VERSION="${MAJOR_VERSION}.${MINOR_VERSION}.${BUILD_VERSION}"
echo "Product version: $PRODUCT_VERSION"

if [ ! -z "$2" ]; then
    BuildId=$2
    DEV_VERSION="${PRODUCT_VERSION}-dev${BuildYear}.${BuildId}"
else
    DEV_VERSION="${PRODUCT_VERSION}-dev${BuildYear}"
fi

# If the build was triggered from a tag, use the tag as the version. Otherwise, set the version to dev.
REGEX='^refs\/tags\/v?([[:digit:]]+)\.([[:digit:]]+)\.([[:digit:]]+)(-.+)?'

# If tag is a release tag, set up release variables.
[[ $BUILD_SOURCEBRANCH =~ $REGEX ]] && define_variable "IsRelease" "true" || define_variable "IsRelease" "false"

# Extract version from the tag.
VERSION=$([[ $BUILD_SOURCEBRANCH =~ $REGEX ]] && echo $(echo $BUILD_SOURCEBRANCH | sed -r 's/'$REGEX'/\1.\2.\3\4/') || echo "$DEV_VERSION")

# Set the AICLIVersion variable in the pipeline.
define_variable "AICLIVersion" "$VERSION"

# Set the AICLINuPkgFileName variable in the pipeline.
define_variable "AICLINuPkgFileName" "Azure.AI.CLI.$VERSION.nupkg"

# At this point, the $VERSION may have a pre-release tag. We need to remove it to get the version that will be used for SemVer.
SEMVER_VERSION=$(echo $VERSION | sed -r 's/-.*//')
define_variable "AICLISemVerVersion" "$SEMVER_VERSION"