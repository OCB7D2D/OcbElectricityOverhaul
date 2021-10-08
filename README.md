# OCB Electricity Overhaul Mod - 7 Days to Die Addon

Electricity done right, enables to connect multiple power sources
to each other. Still every power item can only have one parent
connection, but can lend power from upstream if multiple sources
are connected in line. Also preserves pass-through trigger groups.

## Main features

- Power sources can be connected like every other power item
- Power can be taken from any upstream power source
- Prioritize renewable energy before using gasoline
- Additional grid demand/supply and charge statistics
- Batteries are all charged/discharged when in a bank

<p>
    <img src="Screens/game-stats-generator.jpg" alt="Blocks shown in-game" align="left" height="400"/>
    <img src="Screens/game-stats-battery-bank.jpg" alt="Loot through Bars" align="left" height="400"/>
    <img src="Screens/game-options.jpg" alt="Loot through Bars" align="left" height="200"/>
    <br clear="left"/>
</p>

## Additional recommended DMT mods

- https://github.com/OCB7D2D/ElectricityBugfixes (recommended)
- https://github.com/OCB7D2D/ElectricityWireColors (recommended)
- https://github.com/OCB7D2D/ElectricityNoWires (optional)

## Important Notes

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

### Installation via DMT

You either need [DMT][1] to build your own custom game dll. It is also
the most versatile and safe method, since compatibility is easily
guaranteed as long as everything compile. You can also combine other
DMT mods (as long as they don't touch the base game power system).

### Manual Installation

Alternatively you will need the patched dlls for your exact game version.
I will try to provide them whenever possible via my personal webserver.
These files go into the `7DaysToDie_Data/Managed` folder (make backups!).
If you messed something up, you can always reinstall the game via steam:
`7 Days to Die -> Properties -> Local files -> Verify integrity`.

http://www.ocbnet.ch/OCB7D2D/ElectricityOverhaul/

In the end you must copy four files into the `Data/Managed` folder:

- `0Harmony.dll`
- `Assembly-CSharp.dll`
- `DMT.dll`
- `Mods.dll`

The `Assembly-CSharp.dll` must match your game installation. For
single-player use `sp-*` files, for dedicated server use `dedi-*`.
Single-player has it's files in `7DaysToDie_Data`, while dedicated
servers use `7DaysToDieServer_Data`. If you're specific game version
isn't available you're out of luck and need to use DMT instead.

Once you have installed these files you need to copy the Mods you want
to use into the `Mods` folder of your game (you may need to create it).
Basically go into the `Mods` folder and clone the git repos you want.

### Semantic versioning

We try to use semi/major/minor versioning, while major version increments
mean that the underlying `Assembly-CSharp.dll` had changes. All minor
versions should be compatible without updating the assembly dll.

## Multi-player (Dedicated Server) support

This Mod should work with dedicated servers, although the new algorithm
may lead to higher CPU utilization, specially if a lot of players build
a lot of power sources. Please open an issue here if you run into problems.

## Power distribution logic

Distribution of power always starts at a root power source, one that
doesn't have any further power source connected upstream. From there
the power is distributed to all local power consumers connected to
that power source. Those will then try to take their required power
from all connected upstream power sources in the following order:

- Closest solar power panel
- Closest diesel generator
- Closest battery bank

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

Certain setups can expose "undefined behavior" if the required power
is less then the available power. Consider the following example:

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

There is a new option under "controls" where you can enable/disable the
charging of batteries from batteries. Default is to only charge batteries
from diesel generators or solar panels (self-charge defaults to `false`).

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

## Compatibility

I've developed and tested this Mod against version a19.6b8.

[1]: https://github.com/HAL-NINE-THOUSAND/DMT