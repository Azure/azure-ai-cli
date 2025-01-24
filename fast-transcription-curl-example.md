## `curl --location "https://eastus2.api.cognitive.microsoft.com/speechtotext/transcriptions:transcribe?api-version=2024-05-15-preview" --header "Content-Type: multipart/form-data" --header "Accept: application/json" --header "Ocp-Apim-Subscription-Key: 1cf62c7ee8a24851bb71186b2ec4e449" --form "audio=@test.wav" --form "definition=\"{ \\\"locales\\\":[\\\"en-US\\\"] }\""`

Output:
```
  % Total    % Received % Xferd  Average Speed   Time    Time     Time  Current
                                 Dload  Upload   Total   Spent    Left  Speed

  0     0    0     0    0     0      0      0 --:--:-- --:--:-- --:--:--     0
100 42337    0   350  100 41987    626  75152 --:--:-- --:--:-- --:--:-- 76008
{"duration":1300,"combinedPhrases":[{"text":"This is a test."}],"phrases":[{"offset":80,"duration":960,"text":"This is a test.","words":[{"text":"This","offset":80,"duration":240},{"text":"is","offset":320,"duration":160},{"text":"a","offset":480,"duration":40},{"text":"test.","offset":520,"duration":520}],"locale":"en-US","confidence":0.8856769}]}
```