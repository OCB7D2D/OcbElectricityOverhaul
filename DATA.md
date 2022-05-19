# Some data I gathered during development

This is a loose compilation of some data I gathered
while developing this mod. Might be useful later.

## Vanilla generator

XML Block options:

MaxPower 11250
MaxOutputPerFuel 11250
OutputPerStack 100

Still just 50W per engine (300W with 6 engines)?
The MaxPower is calculated by multiplying by SlotCount.
PowerSource::RefreshPowerStats lerps from 0.5 to 1 via the
quality of each slot item to calculate the `MaxOutput`. A
generator always has a quality of 0, resulting in 0.5*100W.

## Vanilla solar bank

```xml
<property name="OutputPerStack" value="30"/>
<property name="SlotItem" value="solarCell"/>
<property name="MaxPower" value="180"/>
```

## Vanilla battery bank

```xml
<property name="MaxPower" value="400"/>
<property name="InputPerTick" value="5"/>
<property name="ChargePerInput" value="1"/>
<property name="OutputPerStack" value="50"/> - Supported
<property name="OutputPerCharge" value="90"/>
<property name="SlotItem" value="carBattery"/>
```

## Battery MaxUseTimes and dis-/charge capacity

- Tier 1 => 5000 (29w)
- Tier 2 => 6000 (33w)
- Tier 3 => 7000 (37w)
- Tier 4 => 8000 (41w)
- Tier 5 => 9000 (45w)
- Tier 6 => 10000 (50w)

Batteries will alway be only charged at 5W.
But not sure if exchange is really even!?

### Vanilla battery discharge time

To drain a tier 1 battery with
20 Watts takes two in-game day.

### Vanilla battery charge time

To charge a tier 1 battery with
5 Watts takes 4-5 in-game hours.

### Vanilla battery charge fuel

To charge a tier 1 battery takes
only about 2 units of gasoline!?
