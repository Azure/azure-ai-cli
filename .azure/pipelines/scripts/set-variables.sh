#!/bin/bash
# Parameters:
# 1. Day of the year  - from DevOps $(DayOfYear)
# 2. Build run number - from DevOps $(Rev:r)

define_variable () {
    echo "$1=$2"
    echo "##vso[task.setvariable variable=$1;isOutput=true]$2"
}

echo "Source branch: $BUILD_SOURCEBRANCH"

# Determine the product version (major.minor.build).
MAJOR_VERSION="1"
MINOR_VERSION="0"
BUILD_VERSION="0"
if [ ! -z "$1" -a ! -z "$2" ]; then
    DayOfYear=$1
    BuildRevR=$2
    if [ $DayOfYear -gt 0 -a $DayOfYear -le 366 -a $BuildRevR -gt 0 -a $BuildRevR -le 99 ]; then
        let BUILD_VERSION="$DayOfYear * 100 + $BuildRevR"
    else
        >&2 echo "Ignored invalid arguments: DayOfYear ${DayOfYear} BuildRevR ${BuildRevR}"
    fi
fi
PRODUCT_VERSION="${MAJOR_VERSION}.${MINOR_VERSION}.${BUILD_VERSION}"
echo "Product version: $PRODUCT_VERSION"

# If the build was triggered from a tag, use the tag as the version. Otherwise, use the product version.
REGEX='^refs\/tags\/v?([[:digit:]]+)\.([[:digit:]]+)\.([[:digit:]]+)(-.+)?'

# If tag is a release tag, set up release variables.
[[ $BUILD_SOURCEBRANCH =~ $REGEX ]] && define_variable "IsRelease" "true" || define_variable "IsRelease" "false"

# Extract version from the tag.
VERSION=$([[ $BUILD_SOURCEBRANCH =~ $REGEX ]] && echo $(echo $BUILD_SOURCEBRANCH | sed -r 's/'$REGEX'/\1.\2.\3\4/') || echo "$PRODUCT_VERSION")

# Set the AICLIVersion variable in the pipeline.
define_variable "AICLIVersion" "$VERSION"

# Set the AICLINuPkgFileName variable in the pipeline.
define_variable "AICLINuPkgFileName" "Azure.AI.CLI.$VERSION.nupkg"

# At this point, the $VERSION may have a pre-release tag. We need to remove it to get the version that will be used for SemVer.
SEMVER_VERSION=$(echo $VERSION | sed -r 's/-.*//')
define_variable "AICLISemVerVersion" "$SEMVER_VERSION"