echo "Uploading $1 to $2"

az storage blob upload --subscription "Speech Services - DEV - Transcription" --account-name csspeechstorage --container-name "drop" -f "$1" -n "$2"