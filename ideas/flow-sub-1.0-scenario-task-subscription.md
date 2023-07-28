# Select Scenario

Load scenario choices

```markdown
# SCENARIO
Name: *** Loading choices ***
```

==>

User picks scenario

```markdown
# SCENARIO
Name: --------------------------------
      │ Chat (OAI)                            <== default selection
      │ Chat w/ your prompt (OAI)
      │ Chat w/ your data (OAI)
      │ Caption audio (Speech to Text)
      │ Caption images and video (Vision)
      │ Extract text from images (Vision)
      │ Extract text from documents and forms (Language)
      │ Transcribe and analyze calls (Speech, Language)
      │ Translate documents and text (Language)
      │ Summarize documents (Language)
      │ ...more
      --------------------------------
```

==> [select a task](#select-task)

# Select Task

Load task choices

```markdown
# SCENARIO
Name: Chat (OAI)
Task: *** Loading choices ***
```

==>

User picks task

```markdown
# SCENARIO
Name: Chat (OAI)
Task: --------------------------------
      │ Explore interactively           <== default selection
      │ Initialize resources
      │ Generate code
      --------------------------------
```
==> [select a subscription](#select-subscription)

# Select Subscription

Load list of subscription, and determine default selection

```markdown
# SCENARIO
Name: Chat (OAI)
Task: Explore interactively

Subscription: *** Loading choices ***
```

==>

Show all subscriptions, with default at top and selected, user picks

```markdown
# SCENARIO
Name: Chat (OAI)
Task: Explore interactively

Subscription: -----------------------------
              | Speech Services - SDK (rob)    <== default selection
              │ Speech Services - EXP0
              │ Speech Services - EXP1
              │ Speech Services - EXP2
              -----------------------------
```

==>

```markdown
# SCENARIO
Name: Chat (OAI)
Task: Explore interactively

Subscription: Speech Services - SDK (rob) (e72e5254-f265-4e95-9bd2-9ee8e7329051)
```

==> [select or create resource](flow-sub-1.1-ai-select-or-create-resource.md)