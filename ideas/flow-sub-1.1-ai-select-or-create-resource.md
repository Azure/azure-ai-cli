
# AI hub - Select or Create

After [selecting scenario, task, and subscription](flow-sub-1.0-scenario-task-subscription.md) ...  

Load list of hubs, and determine default selection

```markdown
# SCENARIO
Name: Chat (OAI)
Task: Explore interactively

Subscription: Speech Services - SDK (rob) (e72e5254-f265-4e95-9bd2-9ee8e7329051)

# AI HUB
Name: *** Loading choices ***
```

==> if there are no hubs: [confirm creating a new one](#ai-hub---no-hubs)  
or  
==> if there is at least one hub: [confirm/select hub or create a new one](#ai-hub---1-or-more-hubs)

# AI hub - No hubs

If no hubs, show option to confirm creating a new one

```markdown
# SCENARIO
Name: Chat (OAI)
Task: Explore interactively

Subscription: Speech Services - SDK (rob) (e72e5254-f265-4e95-9bd2-9ee8e7329051)

# AI HUB
Name: -----------------------------
      | (Create new)                    <=== default selection
      -----------------------------
```

==> [create a new AI hub](#ai-hub---create-new)

# AI hub - 1 or more hubs

If 1 or more hubs, show list of hubs, with previous default selected, user picks

```markdown
# SCENARIO
Name: Chat (OAI)
Task: Explore interactively

Subscription: Speech Services - SDK (rob) (e72e5254-f265-4e95-9bd2-9ee8e7329051)

# AI HUB
Name: -----------------------------
      | (Create new)
      | robch-hub (westus2, AI hub)     <=== default selection is previous/smart default if match, create new otherwise
      | robch-hub2 (eastus, AI hub)
      | robch-hub3 (eastus, AI hub)
      -----------------------------
```

==> [create a new AI hub](#ai-hub---create-new)  
or
==> [select an AI hub](#ai-hub---selected)

#AI HUB - (Selected)

If a hub was selected, show the choice made

```markdown
# SCENARIO
Name: Chat (OAI)
Task: Explore interactively

Subscription: Speech Services - SDK (rob) (e72e5254-f265-4e95-9bd2-9ee8e7329051)

# AI HUB
Name: robch-hub (westus2, AI hub)
```

==> [select or create deployment](flow-sub-1.2-ai-select-or-create-deployment.md)

#AI HUB - (Create new)

If `(Create new)` was selected, show the choice made

```markdown
# SCENARIO
Name: Chat (OAI)
Task: Explore interactively

Subscription: Speech Services - SDK (rob) (e72e5254-f265-4e95-9bd2-9ee8e7329051)

# AI HUB
Name: (Create new)
```

==> [create hub group](flow-sub-1.3-create-hub-group.md)  
==> [create ai hub](flow-sub-1.4-create-ai-hub.md)  
==> [create ai deployment](flow-sub-1.5-create-ai-deployment.md)