param(
    [string]$inputString
)

$pattern = "AB#(\d+)"

if ($inputString -match $pattern) {
    $number = $Matches[1]
} else {
    $number = 0
}

$number