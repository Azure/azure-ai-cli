# face mock cli.md

## examples

**EXAMPLE 1** - Initialize a person 100% interactively

  ```bash
  vz person init
  ```

**EXAMPLE 2** - Initialize a person, enrolling interactively, saving outputs for use in scripts later

  ```bash
  vz person init --output-person-id @my.person.id --output-group-kind @my.group.kind --output-group-id @my.group.id
  ```

**EXAMPLE 3** - Initialize a person, enrolling non-interactively by specifying all required information

  ```bash
  vz person init --interactive false --name PERSONNAME --files PERSON\*.png --group-id @my.group.id --group-kind large --output-person-id @my.person.id
  ```

**EXAMPLE 4** - Create a Large Person Group, and add one person, with 1 face

  ```bash
  vz person group create --name GROUPNAME --group-kind large --output-person-group-id @my.group.id
  vz person create --name PERSONNAME --group-kind large --group-id @my.group.id --output-person-id @my.person.id
  vz person face add --file image.png --person-id @my.person.id --group-kind large --group-id @my.group.id
  vz person group train --group-id @my.group.id
  ```

**EXAMPLE 5** - Create two people, adding 1 face each; create a Dynamic Person Group referring to both

  ```bash
  vz person create --name PERSONNAME1 --group-kind dynamic --output-person-id @my.person1.id --output-add-person-id @my.person.ids
  vz person create --name PERSONNAME2 --group-kind dynamic --output-person-id @my.person2.id --output-add-person-id @my.person.ids

  vz person face add --person-id @my.person1.id --file person1.png --group-kind dynamic
  vz person face add --person-id @my.person2.id --file person2.png --group-kind dynamic

  vz person group create --name GROUPNAME --group-kind dynamic --add-person-ids @my.person.ids --output-person-group-id @my.group.id
  ```

**EXAMPLE 6** - Identify photos containing persons from a person group

  ```bash
  vz face identify --files *.png --group-id @my.group.id
  ```

**EXAMPLE 7** - Verify each photo contains a specific person

  ```bash
  vz face verify --files *.png --person-id @my.person.id
  ```

**EXAMPLE 8** - Script controlling the unlocking/locking of a door

  ```bash
  :TOP
  vz face identify --camera --group-id @my.group.id
  if not %errorlevel%==0 goto TOP
  call unlock_door.cmd
  call sleep.cmd 5000
  call lock_door.cmd
  goto TOP
  ```

---

## person groups

create/list

```bash
vz person group create
    --name NAME
    [--id GROUP-ID] 
    [--user-data DATA]

    --group-kind KIND (large/dynamic)

    DYNAMIC GROUP
    [--add-person-id ID]
    [--add-person-ids @IDS]

    LARGE GROUP
    [--recognition-model MODEL]

    [--output-person-group-id @@GROUP-ID]

vz person group list
    --group-kind KIND (large/dynamic)

    [--top NUMBER]
    [--start NUMBER]

    [--output-person-group-ids @@GROUP-IDs]
    [--output-last-person-group-id @@GROUP-ID]
```

train/status

```bash
vz person group train
    --id GROUP-ID
    --group-kind KIND (large)
    [--wait [TIMEOUT]]
    

vz person group train status
    --id GROUP-ID
    --group-kind KIND (large)
    [--wait [TIMEOUT]]
```

update/delete

```bash
vz person group update
    --id GROUP-ID
    --name NAME
    [--user-data DATA]

    [--group-kind KIND (large/dynamic)]

    DYANMIC GROUP
    [--add-person-id ID]
    [--add-person-ids @IDs]
    [--remove-person-id ID]
    [--remove-person-ids @IDs]

vz person group delete
    --id GROUP-ID
    [--group-kind KIND (large/dynamic)]
```

---

## persons

create/list

```bash
vz person create
    --name NAME
    [--user-data DATA]

    --group-kind KIND (large/dynamic)

    LARGE GROUP
    [--group-id ID]

    [--output-person-id @@ID]

vz person list
    --group-id GROUP-ID
    [--top NUMBER]
    [--start NUMBER]

    --group-kind KIND (large/dynamic)

    [--output-person-ids @@PERSON-IDs]
    [--output-person-last-id @@PERSON-ID]
```

update/delete

```bash
vz person update
    --person-id ID
    --name NAME
    [--user-data DATA]
    
    --group-kind KIND (large/dynamic)

    LARGE GROUP
    [--group-id ID]

vz person delete
    --person-id ID

    --group-kind KIND (large/dynamic)

    LARGE GROUP
    [--group-id ID]
```

---

## person faces

add/list

```bash
vz person face add
    --person-id ID
    
    --file IMAGE.PNG
    ...or...
    --camera

    --group-kind KIND (large/dynamic)

    DYNAMIC
    --recognition-model MODEL (recognition_01, ... _03)

    LARGE GROUP
    --group-id @group.id

    [--user-data DATA]
    [--target-face "x,y,width,height"]
    [--detection-model MODEL]
    [--output-persisted-face-id @@persisted.id]

vz person face list
    --person-id ID
    [--top NUMBER]
    [--start NUMBER]

    --group-kind KIND (large/dynamic)

    DYNAMIC
    --recognition-model MODEL (recognition_01, ... _03)

    LARGE GROUP
    --group-id @group.id

    [--output-persisted-face-ids @@PERSISTED-FACE-IDs]
    [--output-last-persisted-face-id @@PERSISTED-FACE-ID]

```

update/delete

```bash
vz person face update
    --person-id ID
    --persisted-face-id ID
    --user-data DATA

    DYNAMIC
    --recognition-model MODEL (recognition_01, ... _03)

    LARGE GROUP
    --group-id @group.id

vz person face delete
    --person-id ID
    --persisted-face-id ID

    DYNAMIC
    --recognition-model MODEL (recognition_01, ... _03)

    LARGE GROUP
    --group-id @group.id
```

## STRUCTURE

`vz person`

```markdown
PERSON

  The `vz person` command manages Person Groups, Persons, and Person Faces
  used by the Azure Vision Face Service, enabling identification and
  verification of human faces using the `vz face` command.

USAGE: vz person <command> [...]

COMMANDS

  vz person init [...]            (see: vz help person init)

  vz person group [...]           (see: vz help person group)

  vz person create [...]          (see: vz help person create)
  vz person list [...]            (see: vz help person list)

  vz person update [...]          (see: vz help person update)
  vz person delete [...]          (see: vz help person delete)

  vz person face [...]            (see: vz help person face)

ADDITIONAL TOPICS

  vz help setup
  vz help person overview
  vz help find topics face
  vz help find topics person

```

`vz person group`

```markdown
PERSON GROUP

  The `vz person group` command manages Person Groups containing Persons
  used by the Azure Vision Face Service, enabling identification and
  verification of human faces using the `vz face` command.

USAGE: vz person group <command> [...]

COMMANDS

  vz person group create [...]        (see: vz help person group create)
  vz person group list [...]          (see: vz help person group list)

  vz person group train [...]         (see: vz help person group train)
  vz person group status [...]        (see: vz help person group status)

  vz person group update [...]        (see: vz help person group update)
  vz person group delete [...]        (see: vz help person group delete)

ADDITIONAL TOPICS

  vz help setup
  vz help person overview
  vz help find topics face
  vz help find topics person

```

`vz person face`

```markdown
PERSON FACE

  The `vz person face` command manages Person Faces associated with Persons
  used by the Azure Vision Face Service, enabling identification and
  verification of human faces using the `vz face` command.

USAGE: vz person <command> [...]

COMMANDS

  vz person face add [...]           (see: vz help person face add)
  vz person face list [...]          (see: vz help person face list)

  vz person face update [...]        (see: vz help person face update)
  vz person face delete [...]        (see: vz help person face delete)

ADDITIONAL TOPICS

  vz help setup
  vz help person overview
  vz help find topics face
  vz help find topics person

```
