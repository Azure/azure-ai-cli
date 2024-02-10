
# AI LLM deployment - Select or Create

After [selecting or creating an AI hub](flow-sub-1.1-ai-select-or-create-hub.md) ...

Load list of deployments, and determine default selection

```markdown
# AI LLM DEPLOYMENT
Name: *** Loading choices ***
```

==>

# AI LLM deployment - No deployments

If no deployments, show option to confirm creating a new one

```markdown
# AI LLM DEPLOYMENT
Name: -----------------------------
      | (Create new)                    <=== default selection
      -----------------------------
```

# AI LLM deployment - 1 or more deployment

If 1 or more deployments, show list of deployments, with previous default selected, user picks

```markdown
# AI LLM DEPLOYMENT
Name: -----------------------------
      | (Create new)
      | robch-gpt35-turbo     <=== default selection is previous/smart default if match, create new otherwise
      -----------------------------
```

# AI LLM deployment - (Selected)

If a deployment was selected, show the choice made

```markdown
# AI LLM DEPLOYMENT
Name: robch-gpt35-turbo
```

# AI LLM deployment - (Create new)

If `(Create new)` was selected, show the choice made

```markdown
# AI LLM DEPLOYMENT
Name: (Create new)
```

==> [create deployment](flow-sub-1.5-create-ai-deployment.md)