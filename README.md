# OCB Electricity Overhaul Mod - 7 Days to Die Addon

Electricity done right, enables to connect multiple power sources
to each other. Still every power item can only have one parent
connection, but can lend power from upstream if multiple sources
are connected in line. Also preserves pass-through trigger groups.

https://community.7daystodie.com/topic/25507-electricity-overhaul-mod/

<img src="Screens/in-game.jpg" alt="Blocks shown in-game" height="400" />

## Main features

- Power sources can be connected like every other power item
- Power can be taken from any upstream power source
- Prioritize renewable energy before using gasoline
- Additional grid demand/supply and charge statistics
- Batteries are all charged/discharged when in a bank
- Solar power gradually fades in and out at dawn and dusk
- Decide from which upstream power types to charge batteries

<p>
    <img src="Screens/game-stats-generator.jpg" alt="Blocks shown in-game" align="left" height="400"/>
    <img src="Screens/game-stats-battery-bank.jpg" alt="Loot through Bars" align="left" height="400"/>
    <img src="Screens/game-options.jpg" alt="Loot through Bars" align="left" height="280"/>
    <br clear="left"/>
</p>

## Additional mods (pick the ones you like)

These mods are all additional mods that have been tested with this mod.
But the also work on their own. Therefore pick the ones you like and
install them additionally to the Electricity Overhaul mod.

- https://github.com/OCB7D2D/ElectricityWorkarounds (recommended)
- https://github.com/OCB7D2D/ElectricityWireColors (recommended)
- https://github.com/OCB7D2D/ElectricitySolarRecipes (recommended)
- https://github.com/OCB7D2D/ElectricityLamps (optional)
- https://github.com/OCB7D2D/ElectricityPushButtons (optional)
- https://github.com/OCB7D2D/ElectricityNoWires (optional)

## Important Notes

- Not "Easy Anti-Cheat" compatible, so you need to turn EAC off!
- probably incompatible with anything else touching electricity!
- Needs BepInEx to work to patch the main game dll before startup

This Mod hasn't yet been tested much in the wild and bugs are to be
expected. Some might even not be fixable due to the nature of the
grid distribution. The simple change this mod allows, makes the whole
logic a LOT more complex than the original implementation. Also this
mod hasn't been optimized at all yet and a lot of stuff is calculated
on each tick over and over again. Additionally quite a few additional
fields are transferred between server and client for every power source.

So far I hope the thing will scale well enough already, but that needs
more testing. For that I included a check that will emit a warning
when the update call takes longer than 40ms.

### Installation requires A20BepInExPreloader

- https://github.com/OCB7D2D/A20BepInExPreloader

The required files are also included directly in this repository and if you
load the mod for the very first time, it will try to install the necessary
files for you. Unfortunately this will lead to a broken initial Game-Menu.
Simply restart the game once and the mod should start loading correctly.
Check the console (F1) to see if BepInEx was correctly detected or not.

Alternatively you can also manually install (or uninstall) the files.
For convenience I added two batch files you can use to achieve that.
Check the mod folder (once installed) and you should find them there.

### Linux support

This is even more experimental and needs a bit of work from the user.
Unfortunately there is no magic hook for BepInEx to use, you need to
"tell" the game that it should load additional libraries before the
game starts. To help you with that dilema, this mod will install a
startup script that should do the necessary parts. Unfortunately this
makes the game-launcher obsolete and if you want to change any startup
options, you'd have to do it in the startup script directly. Also you
can't start the game from steam launcher directly anymore!

The relevant startup scripts are:

- startmodclient.sh
- startmodserver.sh

### Semantic versioning

We try to use semi/major/minor versioning, while major version increments
mean that the underlying `Assembly-CSharp.dll` had changes. All minor
versions should be compatible without updating the assembly dll.

## Multi-player (Dedicated Server) support

This Mod should work with dedicated servers, although the new algorithm
may lead to higher CPU utilization, specially if a lot of players build
a lot of power sources. Please open an issue here if you run into problems.

### Server config xml

```xml
<property name="LoadVanillaMap" value="false" />
<property name="BatteryPowerPerUse" value="25" />
<property name="MinPowerForCharging" value="20" />
<property name="FuelPowerPerUse" value="750" />
<property name="PowerPerPanel" value="30" />
<property name="PowerPerEngine" value="50" />
<property name="PowerPerBattery" value="50" />
<property name="ChargePerBattery" value="35" />
```

## Vanilla Map Loading

There is an option to load vanilla maps. If you set this option, we will
assume that additional settings for power sources are not in the save file
yet and skip trying to read them from the save files. The values will be
initialized with the default settings. And once the save files are written
again, these options will then be included ("upgraded" so to speak). So make
sure you only enable this option exactly once, otherwise you may loose your
save files. Probably a good time to make a backup!

## Power distribution logic

Distribution of power always starts at a root power source, one that
doesn't have any further power source connected upstream. From there
the power is distributed to all local power consumers connected to
that power source. Those will then try to take their required power
from all connected upstream power sources in the following order:

- Closest solar power panel
- Closest generator or battery bank

Note that this logic exposes a few quirks when the required power exceeds
the power available. This could of course be further optimized, but it's
questionable if the additional needed CPU power legitimates this.

## Pass-through triggers (as seen in vanilla)

Triggers that are directly connected act like a group, e.g. if one
trigger is active, the whole group is active. E.g. if you connect
multiple motion sensors together, only one has to be triggered/active
to make the power flow through. This includes regular power switches.
In order to make a switch "standalone", just put another power relay
in between to break up the trigger group.

## Power distribution edge cases

Certain setups can expose "undefined behavior" if the available power
is less then the required power. Consider the following example:

```
SolarPanel A -> Diesel Generator B (full) -> Consumers B
             -> Diesel Generator C (empty) -> Consumers C
```

The distribution logic is to first consume the available solar power
in order to preserve gasoline (a rule I hope everybody agrees). Since
we will first distribute power to Consumers B group, they might already
consume all the available solar power, so when we try to distribute power
to Consumer C group, no power is left (since the direct upstream generator
is empty). In this case one could expect that Consumers B uses the available
Diesel Generator B, and Consumer C uses the SolarPanel. But making this
decision in a deeply nested grid gets pretty complex, so calculating this
correctly on each tick can get pretty CPU intensive and complicated.

Please open a PR/issue of you know a better and efficient way to do that!
IMO it is kinda fair to have an erratic behavior in the grid if power is not
fully sufficient. Might also be cool to let some lights flicker randomly?

### Loading battery banks

Loading power banks provided another challenge. Consider the following setup:

```
Diesel Generator (50W) -> Battery Bank -> Consumers (3*20W)
```

The above setup would lead to the situation that one of the three 20 Watt
consumers is only powered 1/3 of the time, while 2/3 of the time the battery
is charged with 10W (2 ticks 10 Watts are charged, 1 tick 20 Watts are consumed).
It doesn't seem that the underlying blocks (e.g. blade trap) would handle this
cleanly, as in my testing this seemed to enable some kind of "power hack".

Battery banks have additional "Charging" in-game options, where you can set
from which power source a specific bank should take power for charging. This
allows to have diesel generators as true backups only in the night, while
batteries are only charged during the day via solar panels.

## Implementation details (devs only)

Here I will go a bit more into the implementation details.

### Detailed logic with pseudo code

At the global level we keep a list of all power sources and every power source
has a list of children, and each child has sub-children, and so on. From that
structure we generate a single list of consumers and sources. We try to do that
in a non recursive fashion to preserve stack space (by doing it via a heap stack).
Not sure if this translates 100% from C++ to C#, but it's elegant none the less.

1) Regenerate all power sources (turn fuel/battery/solar into power)
   - If the available `CurrentPower` is lower than `MaxPower` we burn `fuel`
     * Once the `fuel` is empty, we can't provide power anymore (update event)
2) Find root power sources (by checking for any upstream power source)
   - This could probably be cached, but is currently done for every tick
     * Problem is that triggers are also part in this equation (should they?)
3) Process all (root) power sources (recursive function entry point)
   - Add power source to a stack for downstream consumers to lend from
     * Calculate overall available power for downstream consumers
   - Distribute available power to all local consumers (recursive consumer children)
     * They will consume according to logic ruled out earlier (solar/diesel/battery)
     * Obey trigger groups (skip all children that are not triggered)
     * Update power state for each power source we lend power from
   - Remove power source from stack to return to previous state
   - If the current power source is a battery bank
     * Calculate overall available power left for charging
     * Use the left-over power to charge the battery bank

### Optimizations and Performance

We currently calculate a lot more on each tick than the original code did. So this
mod certainly draws more power from your CPU than vanilla does. I did not yet do
much profiling, but I believe that modern CPUs should be able to handle to load
quite easily. I didn't analyze the original code completely, but I believe they did
take quite a few shortcuts to ensure reliable and scalable performance. Which I don't
think is a bad decision at all, but sometimes people are willing to spend a few more
CPU cycles to get a better experience. Let's see how good this already scales :)

## Changelog

### Version 0.7.5

- Add compatibility for wind power
- Don't persist vanilla map load setting

### Version 0.7.4

- Fixed battery bank charging from specific sources
- Fixed phantom battery bank charging (rounding error)
- Fixed issues with menu options (no more error logged)
- Added unix startup scripts for BepInEx preloader

### Version 0.7.3

- Improve BepInEx installation (now mod-name agnostic)
- Added install/uninstall batch files for manual use

### Version 0.7.2

- Fix issue with Harmony patches across two classes (move into one)  
  Registered Harmony Hooks were called twice with this broken approach

### Version 0.7.1

- Add auto-install for BepInEx requirement (needs manual restart on first load)
- Move game settings for power to own tab to avoid UI overflow

### Version 0.7.0

- Refactor for A20 compatibility

## Compatibility

I've developed and tested this Mod against version a19.6b8.

[1]: https://github.com/HAL-NINE-THOUSAND/DMT