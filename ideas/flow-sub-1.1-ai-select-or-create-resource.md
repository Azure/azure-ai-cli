
# AI Resource - Select or Create

After [selecting scenario, task, and subscription](flow-sub-1.0-scenario-task-subscription.md) ...  

Load list of resources, and determine default selection

```markdown
# SCENARIO
Name: Chat (OAI)
Task: Explore interactively

Subscription: Speech Services - SDK (rob) (e72e5254-f265-4e95-9bd2-9ee8e7329051)

# AI RESOURCE
Name: *** Loading choices ***
```

==> if there are no resources: [confirm creating a new one](#ai-resource---no-resources)  
or  
==> if there is at least one resource: [confirm/select resource or create a new one](#ai-resource---1-or-more-resources)

# AI Resource - No resources

If no resources, show option to confirm creating a new one

```markdown
# SCENARIO
Name: Chat (OAI)
Task: Explore interactively

Subscription: Speech Services - SDK (rob) (e72e5254-f265-4e95-9bd2-9ee8e7329051)

# AI RESOURCE
Name: -----------------------------
      | (Create new)                    <=== default selection
      -----------------------------
```

==> [create a new AI resource](#ai-resource---create-new)

# AI Resource - 1 or more resources

If 1 or more resources, show list of resources, with previous default selected, user picks

```markdown
# SCENARIO
Name: Chat (OAI)
Task: Explore interactively

Subscription: Speech Services - SDK (rob) (e72e5254-f265-4e95-9bd2-9ee8e7329051)

# AI RESOURCE
Name: -----------------------------
      | (Create new)
      | robch-hub (westus2, AI Hub)     <=== default selection is previous/smart default if match, create new otherwise
      | robch-hub2 (eastus, AI Hub)
      | robch-hub3 (eastus, AI Hub)
      -----------------------------
```

==> [create a new AI resource](#ai-resource---create-new)  
or
==> [select an AI resource](#ai-resource---selected)

# AI Resource - (Selected)

If a resource was selected, show the choice made

```markdown
# SCENARIO
Name: Chat (OAI)
Task: Explore interactively

Subscription: Speech Services - SDK (rob) (e72e5254-f265-4e95-9bd2-9ee8e7329051)

# AI RESOURCE
Name: robch-hub (westus2, AI Hub)
```

==> [select or create deployment](flow-sub-1.2-ai-select-or-create-deployment.md)

# AI Resource - (Create new)

If `(Create new)` was selected, show the choice made

```markdown
# SCENARIO
Name: Chat (OAI)
Task: Explore interactively

Subscription: Speech Services - SDK (rob) (e72e5254-f265-4e95-9bd2-9ee8e7329051)

# AI RESOURCE
Name: (Create new)
```

==> [create resource group](flow-sub-1.3-create-resource-group.md)  
==> [create ai resource](flow-sub-1.4-create-ai-resource.md)  
==> [create ai deployment](flow-sub-1.5-create-ai-deployment.md)