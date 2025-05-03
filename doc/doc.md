
# Add Broadcast Tokens
**Add broadcast tokens for any slugcat and apply color styling to the text**

Path：`YourMod`/text/chatlogs/BroadcastID/`lang`.txt

`lang`refers to the abbreviation of the language you are using.
- eng -> English
- chi -> Chinese
- For the remaining naming conventions, refer to _Rain World\RainWorld_Data\StreamingAssets\text_.

Naming requirements:  
By default, the system reads the text file corresponding to the current language setting. If unavailable, it falls back to English, then to other languages. If no matching language file is found, it defaults to the first file (alphabetically) under the _BroadcastID_ folder.  
Example:
Write Chinese text in chi.txt
Write English text in eng.txt

## Color formatting requirements:
To apply color formatting, add `<#XXXXXX>` at the beginning of the line (use the "<" symbol in English input mode, with the hexadecimal color code in the middle).

## In-game placement:
1. Activate devtool (press "O").
2. Press "H" to enter the editing menu, click on "Object," then in the items interface, select the "Tutorial" menu.
3. Place the `CustomChatlogToken` and select your broadcast token.

# Jolly co-op slugcat/slugcat pup switch icon.

Drawing requirements:  
The canvas size for both the slugcat and pup icons should be 37x37 pixels with a transparent background.

Naming requirements:
- Adult：`SlugcatID_off`
- Pup：`SlugcatID_on`
- The SlugcatID should be the ID of your slugcat in the JSON file located under `YourMod/slugbase` folder.

The path for placing the drawn images is: `mods/YourMod/illustrations/`.

# Skin binding to the mod slugcat.

Copy the DMS format skin to the specified path.

Path：`YourMod`/atlas/skin_`<SlugcatID>`/
- SlugcatID is the ID of your slugcat in the JSON file located under the `YourMod/slugbase` folder in `YourMod`.


# Slugbase Features Extension

`<boolean>`: true or false

`integer`: Number with no fractional component

`float`: Number with some fractional component

`color`: The color information should be written in hexadecimal, for example: `33FFCC`.

`<CreatureTemplate.Type / AbstractObjectType>` : Any creature ID or item ID.

`<Player.ObjectGrabability>`: Grab properties.
| Player.ObjectGrabability | explanation |
| ---------- | -------- |
| OneHand | Can be grabbed with one hand. |
| TwoHands | Needs two hands to grab. |
| BigOneHand | Can be grabbed with one hand, but only one can be held at a time (similar to a spear).|
| Drag | For very heavy objects, drag them. |
| CantGrab | Cannot be grabbed. |



## Modify the initial room generation position.

`"spawn_pos": [ <float>, <float> ]`

**example:**
```json5
"spawn_pos": [ 2, 5 ]
```
[ 2, 5 ]The initial coordinates for the slugcat in the room, with the coordinate (2, 5) calculated based on the room's grid.

## Customizable grabable objects/creatures.


```json5
"custom_grabability": [
	{
		"type": <CreatureTemplate.Type / AbstractObjectType>,
		"grabability": <Player.ObjectGrabability>
	},
	...
]
```

**example:**
```json5
"custom_grabability": [
	{
		"type": "Spider",
		"grabability": "OneHand"
	},
	{
		"type": "Oracle",
		"grabability": "OneHand"
	}
]
```

## Customizable edible objects/creatures.

```json5
"custom_edibles": [
	{
		//forbidden_type: Prohibited edible
		"forbidden_type": <CreatureTemplate.Type / AbstractObjectType> 
	},

	{
		"type": <CreatureTemplate.Type / AbstractObjectType?,
		"food_point": <float>
	}
	...
]
```

**example:**
```json5
"custom_edibles": [
	{
		"forbidden_type": "DangleFruit" 
	},
	{
		"type": "DataPearl",
		"food_point": 1.25
	},
	{
		"type": "Oracle",
		"food_point": 1.25
	},
	{
		"type": "Spider",
		"food_point": 1.25
	}
]
```

## Game ending-related.

### Limit the number of cycles.

If the cycle limit is exceeded, death will automatically trigger a settlement.

`"cycle_limited": <integer>`

**example:**
```json5
"cycle_limited": 25
```

### Forced settlement:

If set to true, the game will trigger a settlement directly after exceeding the cycle limit (regardless of whether death occurs or not).

`"cycle_limited_force": <boolean>`

**example:**
```json5
"cycle_limited_force": true
```

### Slugcat selection CG when dying due to insufficient cycles:
`"cycle_limited_force": <string>`

**example:**
```json5
"cycle_limited_force": "MySlugcatCycleDeath"
```

### Lock save file after ascension:
If set to true, the save file will be locked after ascension, and it will not be possible to enter that save file.

`"lock_ascended_force": <boolean>`


**example:**
```json5
"lock_ascended_force": true
```
## Crafting functionality.

The crafting operation is the same as the one for **Gourmand**.

`"craft_items"`：Write the crafting method for an item

`"type"`：Item type.

`"data"`：The default is 0, except for special items like spears.

- For example: The electric spear, which is also a **Spear**, requires changing the number after "data" to 2, and the explosive spear to 1.

`"craft_cost"`：The food point required for crafting.

`"craft_result"`：Crafting result.

```json5
"craft": [
	{
		"craft_items": [
			{
				"type": <AbstractPhysicalObject.Type>,
				"data": <integer> //optional
			}
		],
		"craft_cost": <integer>,
		"craft_result": {
			"type": <AbstractPhysicalObject.Type>,
			"data": <integer> //optional
		}
	},
]
```

**example:**
```json5
//  normal spear +  one food point ==> explosive spear
//  spear + rock + one food point ==> singularity bomb
"craft": [
	{
		"craft_items": [
			{
				"type": "Spear",
				"data": 0
			}
		],
		"craft_cost": 1,
		"craft_result": {
			"type": "Spear",
			"data": 1
		}
	},

	{
		"craft_items": [
			{
				"type": "Spear"
			},
			{
				"type": "Rock"
			}
		],
		"craft_cost": 1,
		"craft_result": {
			"type": "Singularitybomb"
		}
	}
]
```

## Customizable guide overseers and behaviors.

**!!The following functions will only be effective when guide_overseer is non-zero.!!**

-  Example: `"guide_overseer": 1`, for more details, refer to the[Slugbase docs](https://slimecubed.github.io/slugbase/articles/features.html#guide_overseer)

### Overseer color:
`"guide_overseer_color": <color>`

**example:**
```json5
	"guide_overseer_color": "FFFFFF"
```

### Overseer guide region order:
Fill in the region abbreviations, guiding from the lowest priority region to the highest priority region.

`"guide_region_priority": [<string>, ...]`

**example:**
```json5
"guide_region_priority": ["HI","GW","SL"]
```

### Overseer guide symbol:

If using a symbol that exists in the original game:
`"guide_overseer_symbol": <integer>`

If using a custom symbol:
`"guide_overseer_symbol_custom": "image path"`(must be in PNG format).

**example:**
```json5
"guide_overseer_symbol_custom": "atlas/Test"
```
### Point to a special room (for the last region in the guidance path).
`"guide_room_in_region":  [["<region name>","<room name>"], ...]`

```json5
"guide_room_in_region": [
	[ "SL", "SL_AI" ]
],
```

# Uncollapsed iterator behavior modification

`DM`：Spearmaster's timeline: Look To The Moon before the collapse

`SS`：From Spearmaster's to Monk's timeline: Five Pebbles before the collapse

## Behavior Editing：

Path：`YourMod`\slugcatutils\oracle/< any name>.json

Set iterator behavior in this folder

**Do not copy comments into your JSON file**

```json5
{
    "Slugcat": <Slugcat.ID>,	//SlugcatID
    "Oracle": <Oracle.ID>,		//IteratorID
    "Behaviors": [				//Conditions and Events under the Category of Behavior
        {
            "EnterTimes": <integer>,//The following events are triggered when entering for the nth time

			//The following events are triggered in sequence from top to bottom.

            "Events": [
                {
                    "Name": <string>,   //Events can be either custom-defined or orginal
					//_Custom events automatically bind to their corresponding dialogue text: text\oracle\SlugcatID\text_lang\Name(event name).txt

                    "Loop": <boolean>,	//If looping is enabled, the system **will not proceed to the next event
                    "Random": <boolean>,	//Is Random Mode enabled?
                    "MinWait": <integer>,	//Minimum Wait Time (seconds / s)
                    "MaxWait": <integer>	//Maximum Wait Time (seconds / s)
                }
            ]
        },
		...
	]
}
```

**example:**

```json5
{
    "Slugcat": "Prototype",	//YourSlugcatID
    "Oracle": "SS",			//If it's before the collapse, then LTTM would be DM.
    "Behaviors": [			//Conditions and Events under the Category of Behavior
        {
            "EnterTimes": 0,//Trigger the following event group upon first entry
            "Events": [
                {
                    "Name": "EnterDM"//The triggered event is EnterDM
                },
                {
                    "Name": "ThrowOut_Throw_Out"//The triggered event is ThrowOut_Throw_Out
                },
                {
                    "Name": "SPDMChatLog",//This event will randomly select a line of text from SPDMChatLog.txt
                    "Loop": true,//Loop mode enabled. If disabled, the event will trigger once before proceeding to the next event group.
                    "Random": true,//Enable random mode.
                    "MinWait": 5,//Minimum Wait Time (seconds / s)
                    "MaxWait": 10//Maximum Wait Time (seconds / s)
                }
            ]
        },
        {
            "EnterTimes": 1,//Trigger the following event group upon second entry
            "Events": [
                {
                    "Name": "EnterDM1"
                },
                {
                    "Name": "SPDMChatLog",
                    "Loop": true,
                    "Random": true,
                    "MinWait": 5,
                    "MaxWait": 10
                }
            ]
        },
    ]
}
```
| Common Built-in Event Names                 | Meaning                               |
|--------------------------|-------------------------------------|
| ThrowOut_ThrowOut        | Make the slugcat drift towards the shortcut.                         |
| ThrowOut_SecondThrowOut  | Throw the slugcat out                               |
| ThrowOut_Polite_ThrowOut | Politely throw the slugcat out                            |
| ThrowOut_KillOnSight     | After killing the slugcat, throw it out                            |
| Moon_SlumberParty        | DM-exclusive: Enable the iterator to enter a state where the slugcat can read the pearl. When the pearl is thrown, it will be captured and read. |
| Pebbles_SlumberParty     | SS-exclusive: Enable the iterator to enter a state where the slugcat can read the pearl. When the pearl is thrown, it will be captured and read. |

The triggered event will retrieve the corresponding event name's txt file from `text\oracle\SlugcatID\text_`lang`\` and execute the preset behaviors and dialogues within the text.

# Modify the behavior actions of the collapsed iterator.

`SL`：The hunter's line to the saint's line: collapsed The Look to the Moon.

`RW`：Rivulet' line: collapsed Five Pebbles

`CL`：Saint' line: collapsed Five Pebbles

## Behavior Editing：

Path：`YourMod`\slugcatutils\oracle/< any name>.json

Set the iterator's behavior within this folder

**The basic content is the same as the uncollasped iterator, but certain behaviors and states, such as movement, gravity, and closing the shortcut, cannot be implemented. Please do not add them**

## Special Content Modification（Conversation）

`<ConversationName>`：Used to override the iterator's dialogue responses to different events.

| ConversationName         | Corresponding Event             |
|--------------------------|------------------|
| DeadPlayer               | The slugcat dies in front of the iterator      |
| Rain                     | The cycle is about to end          |
| PlayerLeft               | The slugcat leaves while the iterator is speaking.     |
| **LTTM Exclusivity**                 | -                |
| HoldNeuron               | Take the neurons of the LTTM.         |
| ReleaseNeuron            | Release the neurons of the LTTM.         |
| Annoying                 | LTTM is angry             |
| HoldingSSNeuronsGreeting | LTTM greets the slugcat holding the FP neuron  |
| **FP Exclusivity**                 | -                |
| HalcyonStolen            | The Halcyon of FP was stolen          |

**format**:

```json5
{
    "Slugcat": <Slugcat.ID>,	//SLugcatID
    "Oracle": <Oracle.ID>,		//IteratorID
    "Conversations": {
		"<ConversationName>" { 
			"Name": <string> //Dialogue txt Filename
		},
		...
    }
}
```


**example:**
```json5
{
    "Slugcat": "Prototype",
    "Oracle": "SL",
    "Conversations": {
        "HoldNeuron": {
            "Name": "PrototypeHoldNeuron"//Take the LTTM's neurons.
        },
        "ReleaseNeuron": {
            "Name": "PrototypeReleaseNeuron"//Release the LTTM's neurons.
        },
        "DeadPlayer": {
            "Name": "PrototypeDeadPlayer"//The slugcat dies in front of the iterator
        },
        "Rain": {
            "Name": "PrototypeRain"//The cycle is about to end  
        },
        "PlayerLeft": {
            "Name": "PrototypePlayerLeft"//The slugcat leaves while the iterator is speaking.
        },
        "Annoying": {
            "Name": "PrototypeAnnoying"//LTTM is angry
        }
    }
}
```

The triggered event will retrieve the corresponding event name's TXT file from `YourMod`\text\oracle\SlugcatID\text_`lang`\ and execute the preset behaviors and dialogues within the text.

## Dialogue Format

### Basic Format

 - Use `<LINE>` to break lines within the dialogue box.
 - Direct line breaks will switch to the next dialogue box.

### Random Text Pool

Use the `^` symbol at the beginning of a line with the English input method to mark a dialogue group.

**Note:** For an Event, you must add `"Random": true` to use the random text pool. Conversations use the random text pool by default.

```
^Test, this is really tiring<LINE>Especially testing the Overseer’s guidance<LINE>I can’t take it anymore!
^Now I’m writing random dialogues, I don’t know what to write, so let me promote my area mod: Cliffside Garden
This is the second dialogue box!!!!!
^waaaaaaaaaaaaaaaaaa！！！！！
^This is the fifth dialogue group!
```

## Iterator Behavior State Statement Format

### Preset Statement Format

| Format                       | Function                                       |
|--------------------------|------------------------------------------|
| `<string>`               | Regular Dialogue                                     |
| `<string>\|<Number1>\|<Number2>` | Regular dialogue, wait `number1/40` seconds before starting the dialogue, and wait `number2/40` seconds after the dialogue is finished.  |
| `WAIT\|<Number>`             | Wait for `number/40` seconds before proceeding to the next action.                       |
| `IF\|Condition`                 | IF\| Execute if the condition is true                    |
| `ELSE`                   | Execute when the IF condition fails.                                |
| `END`                    | Execute the statement after the condition check ends                               |
| `IF\|&\|Condition\|Condition`        | Both condition 1 and condition 2 must be true                              |
| `IF\|#\|Condition\|Condition`        | Either condition 1 or condition 2 must be true                          |
| `SP\|Command\|Args...`          | Execute specific command behavior                                 |


### Preset Condition

- For `boolean` type conditions, you can directly write `IF|XXX`
- For conditions involving non-`boolean` types, you need to specify the comparison explicitly in the format `IF|XXX (any comparison operator) '<integer/float>'`

| Condition Name                 | Type       | Function              |
|----------------------|----------|-----------------|
| SLHasToldNeuron      | boolean  | Did LTTM warn the Slugcat not to eat neurons |
| SLTotInterrupt       | integer  | Number of times LTTM has been interrupted        |
| SLLikesPlayer        | float    | LTTM's affinity level toward the Slugcat     |
| SLAnnoyances         | integer  | Number of times LTTM has been provoked         |
| SLTotNeuronsGiven    | integer  | Quantity of neurons delivered to LTTM      |
| SLNeuronsLeft        | integer  | Number of remaining neurons owned by LTTM       |
| SLLeaves             | integer  | Number of times slugcat has left LTTM        |
| HasMark              | boolean  | Whether or not slugcat has communication imprint(mark)        |


### Preset command behavior
| Preset behaviors and states                           | Function                           |
|-----------------------------------|------------------------------|
| SP\|gravity\|\<float\>            | Where 0 is off gravity mode and 1 is full gravity mode           |
| SP\|locked                        | Close the shortcut                        |
| SP\|unlocked                      | Open the shortcut                        |
| SP\|work\|\<float\>               | Sets whether the iterator will hitch a ride on the slugcat, with 0 being concerned about the slugcat and 1 being concerned about the job |
| SP\|sound\|\<SoundID\>            | Play the audio file corresponding to the ID                  |
| SP\|turnoff                       | Turn off the background music in the iterator room                 |
| SP\|move\|\<float\>\|\<float\>    | Let the iterator move to specific (x,y) coordinates            |
| SP\|panic                         | Brown-out error                         |
| SP\|resync                        | return to normal                         |
| SP\|karma                         | Give ccommunication imprint(mark) and give karma         |
| SP\|behavior\|\<MovementBehavior> | The content of the MovementBehavior is shown below         |

| MovementBehavior   | Meaning                   |
|--------------------|----------------------|
| Idle               | idle state                 |
| Meditate           | meditate with one's eyes closed                 |
| KeepDistance       | Keeping distance from slugcat             |
| Investigate        | Iterators take a closer look at slugcat      |
| Talk               | Enter the communication state, keep looking at the slugcat and don't move |



**example(EnterDM.txt):**
```
IF|HasMark
Hello,little creature|50|20
ELSE
SP|turnoff
SP|gravity|1
SP|work|0
SP|behavior|Investigate
WAIT|80
SP|karma
END
Welcome to my room.
SP|turnon
SP|work|1
SP|gravity|0
SP|panic
as you see|30
The gravity here is intermittent.
SP|resync
If there's nothing else <LINE> you can leave now |30
Have a nice trip.
```