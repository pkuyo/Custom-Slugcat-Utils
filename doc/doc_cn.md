

# 添加广播代币
**为任何猫添加广播代币，并为文本添加着色**

路径：`YourMod`/text/chatlogs/BroadcastID/`lang`.txt

`lang`为你所使用的语言简写，
- eng -> 英语
- chi -> 中文
- 其余命名规则见Rain World\RainWorld_Data\StreamingAssets/text

命名要求：
默认情况下读取当前语言环境的文本，否则读取英文，再读取其他语言，在未查询到其他语言文本时，默认读取 `BroadcastID` 文件夹下的按首字母排序的第一个文件
例：
编写中文文本为chi.txt
编写英文文本为eng.txt

## 着色格式要求：
在行前添加 `<#XXXXXX>`（使用英文输入法下的"<",中间为16进制颜色信息）

## 游戏内放置：
1. 打开开发者模式（按o）
2. 按h进入编辑菜单，点击object后在物品界面点到Tutorial菜单
3. 放置 `CustomChatlogToken` 并选择你的广播代币

# 惬意合作模式成猫/猫崽切换图标

绘制要求：
成猫、猫崽的图标画布大小均为37X37像素透明底

命名要求：
- 成猫：`SlugcatID(你的猫ID)_off`
- 猫崽：`SlugcatID(你的猫ID)_on`
- SlugcatID为`YourMod`/slugbase文件夹底下的json内你的蛞蝓猫ID

绘制的图片放置路径：mods/YourMod/illustrations/

# 皮肤绑定mod猫

将dms格式皮肤复制到指定路径下

路径：`YourMod`/atlas/skin_`<SlugcatID(蛞蝓猫ID)>`/
- SlugcatID为`YourMod`/slugbase文件夹底下的json内你的蛞蝓猫ID


# Slugbase Features扩展

`<boolean>`: true(真) 或 false(假)

`integer`: 整数

`float`:小数

`color`: 使用16进制写的颜色信息，比如：33FFCC

`<CreatureTemplate.Type / AbstractObjectType>` : 任意生物ID或物品ID

`<Player.ObjectGrabability>`: 抓取属性
| Player.ObjectGrabability | 解释 |
| ---------- | -------- |
| OneHand | 一只手即可抓取 |
| TwoHands | 需要两只手抓取 |
| BigOneHand | 一只手可抓取，但是只能拿一个（类似矛）|
| Drag | 针对很沉的物体，拖动 |
| CantGrab | 无法抓取 |



## 初始房间生成位置修改

`"spawn_pos": [ <float>, <float> ]`

**例子:**
```json5
"spawn_pos": [ 2, 5 ]
```
[ 2, 5 ]为蛞蝓猫在房间内的初始坐标，2,5坐标由房间内网格计算。

## 自定义可抓取物体/生物


```json5
"custom_grabability": [
	{
		"type": <CreatureTemplate.Type / AbstractObjectType>,
		"grabability": <Player.ObjectGrabability>
	},
	...
]
```

**例子:**
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

## 自定义食用物体/生物

```json5
"custom_edibles": [
	{
		//forbidden_type 禁止食用
		"forbidden_type": <CreatureTemplate.Type / AbstractObjectType> 
	},

	{
		"type": <CreatureTemplate.Type / AbstractObjectType?,
		"food_point": <float>
	}
	...
]
```

**例子:**
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

## 游戏结局相关

### 限制雨循环数量

超过cycle_limited死亡自动结算

`"cycle_limited": <integer>`

**例子:**
```json5
"cycle_limited": 25
```

### 强制结算:

若为true超过cycle_limited直接结算（无论死亡与否）

`"cycle_limited_force": <boolean>`

**例子:**
```json5
"cycle_limited_force": true
```

### 轮回不足死亡时的选猫CG:
`"cycle_limited_force": <string>`

**例子:**
```json5
"cycle_limited_force": "MySlugcatCycleDeath"
```

### 飞升后锁档:
若为true飞升后锁档，无法进入该存档

`"lock_ascended_force": <boolean>`


**例子:**
```json5
"lock_ascended_force": true
```
## 合成功能

合成操作同饕餮一致

`"craft_items"`：下写一个物品的合成方式

`"type"`：物品类型

`"data"`：默认为0，除矛类的特殊物品

- 例如：同为Spear的电矛需将data后的数字修改为2，炸矛为1


`"craft_cost"`：合成所需消耗的饱食度

`"craft_result"`：合成结果

```json5
"craft": [
	{
		"craft_items": [
			{
				"type": <AbstractPhysicalObject.Type>,
				"data": <integer> //可选
			}
		],
		"craft_cost": <integer>,
		"craft_result": {
			"type": <AbstractPhysicalObject.Type>,
			"data": <integer> //可选
		}
	},
]
```

**例子:**
```json5
//两个合成
//  普通矛 + 一饱食度 ==> 炸矛
//  矛 + 石头 + 一饱食度 ==> 奇点炸弹
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

## 自定义引导监视者及行为

**!!以下功能均需要在guide_overseer为非零时有效!!**

-  例子:`"guide_overseer":1`，详见[Slugbase docs](https://slimecubed.github.io/slugbase/articles/features.html#guide_overseer)

#### 监视者颜色:
`"guide_overseer_color": <color>`

**例子:**
```json5
	"guide_overseer_color": "FFFFFF"
```

### 监视者指引区域顺序:
填写区域缩写，从低优先级区域向高优先级区域引导

`"guide_region_priority": [<string>, ...]`

**例子:**
```json5
"guide_region_priority": ["HI","GW","SL"]
```

### 监视者指引符号:

如果使用原游戏存在的符号则：
`"guide_overseer_symbol": <integer>`

如果使用自定义符号则：
`"guide_overseer_symbol_custom": "图片路径"`（必须为png格式）

**例子:**
```json5
"guide_overseer_symbol_custom": "atlas/Test"
```
#### 指向特殊房间（针对指引路径的最后一个区域）
`"guide_room_in_region":  [["<区域名称>","<房间名称>"], ...]`

```json5
"guide_room_in_region": [
	[ "SL", "SL_AI" ]
],
```

# 未坍塌迭代器的行为动作修改

`DM`：矛大师线未坍塌的仰望皓月

`SS`：矛大师线至僧侣线未坍塌的五块卵石

## 行为编辑：

路径：`YourMod`\slugcatutils\oracle\<任意名称>.json

在该文件夹下设置迭代器的行为



**请勿将注释一并复制进你的json里**

```json5
{
    "Slugcat": <Slugcat.ID>,	//蛞蝓猫ID
    "Oracle": <Oracle.ID>,		//迭代器ID
    "Behaviors": [				//行为下设条件与事件
        {
            "EnterTimes": <integer>,//第几次进入时触发下列事件

			//以下事件按排列顺序从上至下触发

            "Events": [
                {
                    "Name": <string>,   //可以是自定义事件也可以是原版事件
					//自定义事件会自动读取对应对话文本: text\oracle\SlugcatID\text_lang\Name(事件名称).txt

                    "Loop": <boolean>,	//是否循环，循环则不会进入下一个事件
                    "Random": <boolean>,	//是否开启随机模式
                    "MinWait": <integer>,	//最小等待时间，计时为秒/s
                    "MaxWait": <integer>	//最大等待时间，计时为秒/s
                }
            ]
        },
		...
	]
}
```

**例子:**

```json5
{
    "Slugcat": "Prototype",	//你的蛞蝓猫ID
    "Oracle": "SS",			//若为坍塌前月姐则为DM
    "Behaviors": [			//行为下设条件与事件
        {
            "EnterTimes": 0,//首次进入时触发以下事件组
            "Events": [
                {
                    "Name": "EnterDM"//触发事件为EnterDM
                },
                {
                    "Name": "ThrowOut_Throw_Out"//触发事件为ThrowOut_Throw_Out
                },
                {
                    "Name": "SPDMChatLog",//触发事件为SPDMChatLog，该事件将从SPDMChatLog.txt里随机抽取文本
                    "Loop": true,//是否开启循环模式若关闭则触发一次后进入下一个事件内容zu
                    "Random": true,//是否开启随机模式
                    "MinWait": 5,//最小等待时间，计时为秒/s
                    "MaxWait": 10//最大等待时间，计时为秒/s
                }
            ]
        },
        {
            "EnterTimes": 1,//第二次进入时触发以下事件组
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
| 常用内置事件名称                 | 含义                                  |
|--------------------------|-------------------------------------|
| ThrowOut_ThrowOut        | 使蛞蝓猫向管道出口飘动                         |
| ThrowOut_SecondThrowOut  | 扔出蛞蝓猫                               |
| ThrowOut_Polite_ThrowOut | 礼貌地扔出蛞蝓猫                            |
| ThrowOut_KillOnSight     | 杀死蛞蝓猫后扔出                            |
| Moon_SlumberParty        | DM专用，使迭代器进入可以给蛞蝓猫阅读珍珠的状态，扔出珍珠会被捕获阅读 |
| Pebbles_SlumberParty     | SS专用，使迭代器进入可以给蛞蝓猫阅读珍珠的状态，扔出珍珠会被捕获阅读 |

触发事件将从 text\oracle\SlugcatID\text_`lang`\
调取对应事件名称的txt文本
并执行文本内的预设行为与对话

# 坍塌迭代器的行为动作修改

`SL`：猎手线至圣徒线已坍塌的仰望皓月

`RW`：溪流线已坍塌的五块卵石

`CL`：圣徒线已坍塌的五块卵石

## 行为编辑：

路径：`YourMod`\slugcatutils\oracl<任意名称>.json

在该文件夹下设置迭代器的行为

**基本内容同未崩塌迭代器一致，部分行为与状态如移动、重力、关闭管道口等无法做出，请勿添加进去**

### 特殊内容变更（Conversation）

`<ConversationName>`：用于覆盖迭代器对不同事件反应时的对话

| ConversationName         | 对应事件             |
|--------------------------|------------------|
| DeadPlayer               | 蛞蝓猫在迭代器面前死掉      |
| Rain                     | 雨循环即将结束          |
| PlayerLeft               | 蛞蝓猫在迭代器讲话时离开     |
| **月姐独占**                 | -                |
| HoldNeuron               | 拿取月姐的神经元         |
| ReleaseNeuron            | 放下月姐的神经元         |
| Annoying                 | 月姐生气             |
| HoldingSSNeuronsGreeting | 月姐问候拿着FP神经元的蛞蝓猫  |
| **FP独占**                 | -                |
| HalcyonStolen            | FP被偷赞美诗          |

**格式**:

```json5
{
    "Slugcat": <Slugcat.ID>,	//蛞蝓猫ID
    "Oracle": <Oracle.ID>,		//迭代器ID
    "Conversations": {
		"<ConversationName>" { 
			"Name": <string> //对话txt的名称
		},
		...
    }
}
```


**例子:**
```json5
{
    "Slugcat": "Prototype",
    "Oracle": "SL",
    "Conversations": {
        "HoldNeuron": {
            "Name": "PrototypeHoldNeuron"//拿取月姐的神经元
        },
        "ReleaseNeuron": {
            "Name": "PrototypeReleaseNeuron"//放下月姐的神经元
        },
        "DeadPlayer": {
            "Name": "PrototypeDeadPlayer"//蛞蝓猫在月姐面前死掉
        },
        "Rain": {
            "Name": "PrototypeRain"//雨循环即将结束
        },
        "PlayerLeft": {
            "Name": "PrototypePlayerLeft"//蛞蝓猫在月姐讲话时离开
        },
        "Annoying": {
            "Name": "PrototypeAnnoying"//月姐生气
        }
    }
}
```

触发事件将从 `YourMod`\text\oracle\SlugcatID\text_`lang`\
调取对应事件名称的txt文本
并执行文本内的预设行为与对话

## 对话格式

### 基本格式

 - 对话框内分行使用 `<LINE>`
 - 直接换行会切换到下一个对话框

### 随机文本池

使用英文输入法下的 `^` 并填写在行首，用来标志一个对话组

**注意:** 对于Event必须添加`"Random":true`才可使用随机文本池，Conversation默认使用随机文本池。

```
^测试这个真的很累<LINE>特别是测试监视者指引<LINE>我不活啦！
^现在在写随机对话，我不知道写啥了，于是宣传下我的区域mod：Cliffside Garden
这是第二个对话框！！！！
^waaaaaaaaaaaaaaaaaa！！！！！
^这是第五个对话组！
```

## 迭代器行为状态语句格式

### 预设语句语法

| 格式                       | 功能                                       |
|--------------------------|------------------------------------------|
| `<string>`               | 常规对话                                     |
| `<string>\|<数字1>\|<数字2>` | 常规对话，在对话开始前等待`数字1/40`秒，对话完成后等待`数字2/40`秒  |
| `WAIT\|<数字>`             | 等待`数字/40`秒后进行下一个行为                       |
| `IF\|条件`                 | 如果判断，IF\|条件判断为true则执行                    |
| `ELSE`                   | IF判断失败时执行                                |
| `END`                    | 结束判断后执行的语句                               |
| `IF\|&\|条件1\|条件2`        | 同时满足条件1与条件2                              |
| `IF\|#\|条件1\|条件2`        | 满足条件1与条件2中的任意一条                          |
| `SP\|指令\|参数...`          | 执行特定指令行为                                 |



### 预设条件

- 对于 `boolean`类型的条件 直接写 `IF|XXX` 即可
- 对于 非`boolean`类型的条件 需要写 `IF|XXX >(任意比较运算符) '<integer/float>'`

| 条件名称                 | 类型       | 功能              |
|----------------------|----------|-----------------|
| SLHasToldNeuron      | boolean  | 月姐是否告诉蛞蝓猫不要吃神经元 |
| SLTotInterrupt       | integer  | 月姐被打断的次数        |
| SLLikesPlayer        | float    | 月姐对蛞蝓猫的好感程度     |
| SLAnnoyances         | integer  | 惹恼月姐的次数         |
| SLTotNeuronsGiven    | integer  | 给予月姐神经元的数量      |
| SLNeuronsLeft        | integer  | 月姐剩余神经元数量       |
| SLLeaves             | integer  | 蛞蝓猫离开月姐次数       |
| HasMark              | boolean  | 是否拥有交流印记        |


### 预设指令行为
| 预设行为与状态                           | 功能                           |
|-----------------------------------|------------------------------|
| SP\|gravity\|\<float\>            | 其中0为关闭重力模式，1为全重力模式           |
| SP\|locked                        | 关闭管道口                        |
| SP\|unlocked                      | 解锁管道口                        |
| SP\|work\|\<float\>               | 设置迭代器是否会搭理蛞蝓猫，0为关注蛞蝓猫，1为关注工作 |
| SP\|sound\|\<SoundID\>            | 播放对应ID的音频文件                  |
| SP\|turnoff                       | 关闭迭代器房间的背景音乐                 |
| SP\|move\|\<float\>\|\<float\>    | 让迭代器移动至具体 (x,y)坐标            |
| SP\|karma                  | 给予沟通标记并给业力                               |
| SP\|panic                         | 断电报错                         |
| SP\|resync                        | 恢复正常                         |
| SP\|behavior\|\<MovementBehavior> | MovementBehavior内容见下         |

| MovementBehavior   | 含义                   |
|--------------------|----------------------|
| Idle               | 空闲状态                 |
| Meditate           | 闭眼冥想                 |
| KeepDistance       | 与蛞蝓猫保持距离             |
| Investigate        | 迭代器对蛞蝓猫进行仔细观察        |
| Talk               | 进入交流状态，保持看着蛞蝓猫，不进行移动 |



**例如(EnterDM.txt):**
```
IF|HasMark
你好啊小家伙|50|20
ELSE
SP|turnoff
SP|gravity|1
SP|work|0
SP|behavior|Investigate
WAIT|80
SP|karma
END
欢迎来到我的演算室
SP|turnon
SP|work|1
SP|gravity|0
SP|panic
如你所见|30
这里的重力时断时续
SP|resync
如果没有什么其他事的话<LINE>你现在可以离开了|30
祝你旅途愉快
```