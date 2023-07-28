
# AI Resource Deployment - Select or Create

After [selecting or creating an AI resource](flow-sub-1.1-ai-select-or-create-resource.md) ...

Load list of deployments, and determine default selection

```markdown
# AI RESOURCE DEPLOYMENT
Name: *** Loading choices ***
```

==>

# AI Resource Deployment - No deployments

If no deployments, show option to confirm creating a new one

```markdown
# AI RESOURCE DEPLOYMENT
Name: -----------------------------
      | (Create new)                    <=== default selection
      -----------------------------
```

# AI Resource Deployment - 1 or more deployment

If 1 or more deployments, show list of deployments, with previous default selected, user picks

```markdown
# AI RESOURCE DEPLOYMENT
Name: -----------------------------
      | (Create new)
      | robch-gpt35-turbo     <=== default selection is previous/smart default if match, create new otherwise
      -----------------------------
```

# AI Resource Deployment - (Selected)

If a deployment was selected, show the choice made

```markdown
# AI RESOURCE DEPLOYMENT
Name: robch-gpt35-turbo
```

# AI Resource Deployment - (Create new)

If `(Create new)` was selected, show the choice made

```markdown
# AI RESOURCE DEPLOYMENT
Name: (Create new)
```

==> [create deployment](flow-sub-1.5-create-ai-deployment.md)